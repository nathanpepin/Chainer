using Chainer.Abstractions;
using Chainer.Configuration;
using Chainer.Execution;

namespace Chainer.Persistence;

/// <summary>
///     A thread-safe, in-memory implementation of the <see cref="IChainRepository" /> interface
///     that stores chain messages and execution logs in concurrent dictionaries.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="InMemoryChainRepository" /> provides a lightweight, non-persistent
///         implementation of the chain repository interface. It stores all data in memory
///         using thread-safe concurrent collections, making it suitable for:
///         <list type="bullet">
///             <item>Development and testing scenarios</item>
///             <item>Single-instance applications without persistence requirements</item>
///             <item>Prototype or proof-of-concept implementations</item>
///             <item>Unit testing or integration testing</item>
///         </list>
///     </para>
///     <para>
///         As an in-memory implementation, all data is lost when the application restarts.
///         This class is not suitable for production scenarios where chain definitions or
///         execution history need to persist across application restarts or be shared
///         across multiple instances.
///     </para>
///     <para>
///         This implementation is designed to be a drop-in replacement for database-backed
///         repositories during development or testing, with identical behavior except for
///         persistence. It can also serve as a reference implementation and template for
///         creating custom repository implementations.
///     </para>
///     <para>
///         Key characteristics of this implementation include:
///         <list type="bullet">
///             <item>Thread-safety through the use of <see cref="ConcurrentDictionary{TKey, TValue}" /></item>
///             <item>Minimal overhead compared to database operations</item>
///             <item>Simplified implementation that focuses on correct behavior rather than persistence</item>
///             <item>Additive approach to saving messages (new messages are added to existing ones)</item>
///             <item>Direct object reference sharing for execution logs (updates are in-place)</item>
///         </list>
///     </para>
///     <para>
///         For production use cases requiring persistence, implement a custom repository using
///         a database or other durable storage technology, following the same interface contract.
///     </para>
/// </remarks>
public sealed class InMemoryChainRepository : IChainRepository
{
    /// <summary>
    ///     The standard GUID used to identify the default chain across all repository implementations.
    /// </summary>
    public static readonly Guid DefaultChainGuid = new("6ae8a81e-d7f0-43d2-9617-dfd4528b0c89");

    /// <summary>
    ///     Gets the thread-safe dictionary that stores chain messages, keyed by chain ID.
    /// </summary>
    public ConcurrentDictionary<Guid, List<ChainMessage>> Messages { get; } = new();

    /// <summary>
    ///     Gets the thread-safe dictionary that stores chain execution logs, keyed by chain ID.
    /// </summary>
    public ConcurrentDictionary<Guid, List<ChainExecutionLog>> ExecutionLogs { get; } = new();

    /// <summary>
    ///     Retrieves the messages that define the default chain.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result containing
    ///     the list of chain messages or an error
    /// </returns>
    public Task<Result<List<ChainMessage>>> GetDefaultChainMessagesAsync(CancellationToken cancellationToken = default)
    {
        return GetChainMessagesAsync(DefaultChainGuid, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the messages that define a chain identified by a string identifier.
    /// </summary>
    /// <param name="chainId">A string that uniquely identifies the chain</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result containing
    ///     the list of chain messages or an error
    /// </returns>
    public Task<Result<List<ChainMessage>>> GetChainMessagesAsync(string chainId, CancellationToken cancellationToken = default)
    {
        var id = GuidFromString.CreateDeterministicGuid(chainId);
        return GetChainMessagesAsync(id, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the messages that define a chain identified by a GUID.
    /// </summary>
    /// <param name="chainId">The GUID that uniquely identifies the chain</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result containing
    ///     the list of chain messages or an error
    /// </returns>
    public Task<Result<List<ChainMessage>>> GetChainMessagesAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(Result<List<ChainMessage>>.Failure("Operation was canceled"));

        if (Messages.TryGetValue(chainId, out var messages)) return Task.FromResult(Result<List<ChainMessage>>.Success(messages));

        return Task.FromResult(Result<List<ChainMessage>>.Success([]));
    }

    /// <summary>
    ///     Saves a collection of messages to the default chain.
    /// </summary>
    /// <param name="messages">The chain messages to save</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    public Task<Result> SaveToDefaultChainMessagesAsync(IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default)
    {
        var chainMessages = messages as ChainMessage[] ?? messages.ToArray();
        foreach (var message in chainMessages) message.ChainId = DefaultChainGuid;

        return SaveChainMessagesAsync(DefaultChainGuid, chainMessages, cancellationToken);
    }

    /// <summary>
    ///     Saves a collection of messages to a chain identified by a string identifier.
    /// </summary>
    /// <param name="chainId">A string that uniquely identifies the chain</param>
    /// <param name="messages">The chain messages to save</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    public Task<Result> SaveChainMessagesAsync(string chainId, IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default)
    {
        var id = GuidFromString.CreateDeterministicGuid(chainId);
        return SaveChainMessagesAsync(id, messages, cancellationToken);
    }

    /// <summary>
    ///     Saves a collection of messages to a chain identified by a GUID.
    /// </summary>
    /// <param name="chainId">The GUID that uniquely identifies the chain</param>
    /// <param name="messages">The chain messages to save</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    public Task<Result> SaveChainMessagesAsync(Guid chainId, IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(Result.Failure("Operation was canceled"));

        try
        {
            var messagesList = messages.ToList();
            Messages.AddOrUpdate(
                chainId,
                _ => messagesList,
                (_, existingMessages) =>
                {
                    existingMessages.AddRange(messagesList);
                    return existingMessages;
                });

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Failed to save chain messages: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Saves a collection of chain execution logs to the repository.
    /// </summary>
    /// <param name="chainExecutionLogs">The execution logs to save</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    public Task<Result> SaveChainExecutionLogs(IEnumerable<ChainExecutionLog> chainExecutionLogs, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(Result.Failure("Operation was canceled"));

        try
        {
            foreach (var log in chainExecutionLogs)
            {
                ExecutionLogs.TryAdd(log.ChainId, []);
                ExecutionLogs.TryGetValue(log.ChainId, out var messages);

                if (messages is null) throw new Exception("Chain execution logs cannot be null");

                messages.Add(log);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Failed to save execution logs: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Updates the status of a chain execution log.
    /// </summary>
    /// <param name="chainExecutionLog">The log to update</param>
    /// <param name="status">The new status to set</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    public Task<Result> UpdateChainExecutionLog(
        ChainExecutionLog chainExecutionLog,
        ChainMessageStatus status,
        CancellationToken cancellationToken = default)
    {
        chainExecutionLog.Status = status;
        return Task.FromResult(Success());
    }

    /// <summary>
    ///     Updates a chain execution log with a new status and context data.
    /// </summary>
    /// <param name="chainExecutionLog">The log to update</param>
    /// <param name="status">The new status to set</param>
    /// <param name="beforeExecution">The JSON-serialized context state before execution</param>
    /// <param name="afterExecution">The JSON-serialized context state after execution</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    public Task<Result> UpdateChainExecutionLog(
        ChainExecutionLog chainExecutionLog,
        ChainMessageStatus status,
        string? beforeExecution,
        string afterExecution,
        CancellationToken cancellationToken = default)
    {
        chainExecutionLog.Status = status;
        chainExecutionLog.BeforeJson = beforeExecution;
        chainExecutionLog.AfterJson = afterExecution;
        return Task.FromResult(Success());
    }

    /// <summary>
    ///     Retrieves the execution logs for a chain identified by a GUID.
    /// </summary>
    /// <param name="chainId">The GUID that uniquely identifies the chain</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a list of execution logs
    /// </returns>
    public Task<List<ChainExecutionLog>> GetChainExecutionLogs(Guid chainId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ExecutionLogs.TryGetValue(chainId, out var logs) ? logs : []);
    }

    /// <summary>
    ///     Retrieves the execution logs for the default chain.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a list of execution logs
    /// </returns>
    public Task<List<ChainExecutionLog>> GetDefaultChainExecutionLogs(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ExecutionLogs.TryGetValue(DefaultChainGuid, out var logs) ? logs : []);
    }

    /// <summary>
    ///     Updates a collection of chain execution logs.
    /// </summary>
    /// <param name="chainExecutionLogs">The logs to update</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    public Task<Result> UpdateChainExecutionLog(IEnumerable<ChainExecutionLog> chainExecutionLogs, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Success());
    }
}