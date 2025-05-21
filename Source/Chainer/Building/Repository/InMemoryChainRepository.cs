namespace Chainer.Building.Repository;

/// <summary>
///     A thread-safe, in-memory implementation of the <see cref="IChainRepository"/> interface
///     that stores chain messages and execution logs in concurrent dictionaries.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="InMemoryChainRepository"/> provides a lightweight, non-persistent
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
///             <item>Thread-safety through the use of <see cref="ConcurrentDictionary{TKey, TValue}"/></item>
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
    /// <remarks>
    ///     <para>
    ///         This predefined GUID serves as a well-known identifier for the default chain,
    ///         ensuring consistent behavior across different repository implementations.
    ///         Methods that work with the default chain, such as <see cref="GetDefaultChainMessagesAsync"/>
    ///         and <see cref="SaveToDefaultChainMessagesAsync"/>, use this GUID internally.
    ///     </para>
    ///     <para>
    ///         The use of a consistent GUID rather than a string name or other identifier
    ///         ensures compatibility with the repository's primary storage model, which
    ///         uses GUIDs as keys.
    ///     </para>
    ///     <para>
    ///         For custom repositories, this constant should be used to maintain consistent
    ///         behavior with the standard implementation.
    ///     </para>
    /// </remarks>
    public static readonly Guid DefaultChainGuid = new("6ae8a81e-d7f0-43d2-9617-dfd4528b0c89");

    /// <summary>
    ///     Gets the thread-safe dictionary that stores chain messages, keyed by chain ID.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This dictionary serves as the primary storage for chain definitions. Each key
    ///         is a chain ID (GUID), and each value is a list of <see cref="ChainMessage"/>
    ///         objects that define the handlers in that chain.
    ///     </para>
    ///     <para>
    ///         The use of <see cref="ConcurrentDictionary{TKey, TValue}"/> ensures thread-safety
    ///         for operations that might occur simultaneously from multiple threads, such as
    ///         reading and writing chain definitions.
    ///     </para>
    ///     <para>
    ///         This property is exposed publicly to facilitate testing and debugging, but
    ///         direct manipulation is not recommended in normal usage. Instead, use the
    ///         repository's methods to interact with the stored messages.
    ///     </para>
    /// </remarks>
    public ConcurrentDictionary<Guid, List<ChainMessage>> Messages { get; } = new();

    /// <summary>
    ///     Gets the thread-safe dictionary that stores chain execution logs, keyed by chain ID.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This dictionary serves as the primary storage for execution history. Each key
    ///         is a chain ID (GUID), and each value is a list of <see cref="ChainExecutionLog"/>
    ///         objects that record the execution of handlers in that chain.
    ///     </para>
    ///     <para>
    ///         The use of <see cref="ConcurrentDictionary{TKey, TValue}"/> ensures thread-safety
    ///         for operations that might occur simultaneously from multiple threads, such as
    ///         updating execution status while retrieving execution history.
    ///     </para>
    ///     <para>
    ///         This property is exposed publicly to facilitate testing and debugging, but
    ///         direct manipulation is not recommended in normal usage. Instead, use the
    ///         repository's methods to interact with the stored logs.
    ///     </para>
    /// </remarks>
    public ConcurrentDictionary<Guid, List<ChainExecutionLog>> ExecutionLogs { get; } = new();

    /// <summary>
    ///     Retrieves the messages that define the default chain.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result containing
    ///     the list of chain messages or an error
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method simply delegates to <see cref="GetChainMessagesAsync(Guid, CancellationToken)"/>
    ///         with <see cref="DefaultChainGuid"/> as the chain ID. It provides a convenient
    ///         way to access the default chain without needing to know its specific GUID.
    ///     </para>
    ///     <para>
    ///         In this implementation, no actual asynchronous operations occur since all data
    ///         is stored in memory, but the method maintains the asynchronous signature for
    ///         compatibility with the interface and to allow for seamless substitution with
    ///         database-backed implementations.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     <para>
    ///         This method converts the string identifier to a deterministic GUID using
    ///         <see cref="GuidFromString.CreateDeterministicGuid(string)"/> and then
    ///         delegates to <see cref="GetChainMessagesAsync(Guid, CancellationToken)"/>.
    ///     </para>
    ///     <para>
    ///         The conversion to a deterministic GUID ensures that the same string always
    ///         maps to the same GUID, allowing for consistent identification of chains
    ///         using human-readable names.
    ///     </para>
    ///     <para>
    ///         In this implementation, no actual asynchronous operations occur since all data
    ///         is stored in memory, but the method maintains the asynchronous signature for
    ///         compatibility with the interface.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     <para>
    ///         This is the core method for retrieving chain definitions. It checks if the
    ///         specified chain ID exists in the <see cref="Messages"/> dictionary and returns
    ///         the associated list of messages.
    ///     </para>
    ///     <para>
    ///         If the chain ID is not found, an empty list is returned rather than an error.
    ///         This behavior is consistent with the repository interface contract, which
    ///         specifies that a missing chain is not considered an error condition.
    ///     </para>
    ///     <para>
    ///         The method respects the provided cancellation token, returning a failure result
    ///         if cancellation is requested before the operation completes.
    ///     </para>
    ///     <para>
    ///         In this implementation, the operation is effectively synchronous since it's
    ///         just a dictionary lookup, but the method maintains the asynchronous signature
    ///         for compatibility with the interface and to allow for seamless substitution
    ///         with database-backed implementations.
    ///     </para>
    /// </remarks>
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
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method ensures that all provided messages have their ChainId property
    ///         set to <see cref="DefaultChainGuid"/> and then delegates to
    ///         <see cref="SaveChainMessagesAsync(Guid, IEnumerable{ChainMessage}, CancellationToken)"/>.
    ///     </para>
    ///     <para>
    ///         This method modifies the ChainId property of each message, ensuring they
    ///         all belong to the default chain regardless of their original assignment.
    ///     </para>
    ///     <para>
    ///         In this implementation, no actual asynchronous operations occur since all data
    ///         is stored in memory, but the method maintains the asynchronous signature for
    ///         compatibility with the interface.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     <para>
    ///         This method converts the string identifier to a deterministic GUID using
    ///         <see cref="GuidFromString.CreateDeterministicGuid(string)"/> and then
    ///         delegates to <see cref="SaveChainMessagesAsync(Guid, IEnumerable{ChainMessage}, CancellationToken)"/>.
    ///     </para>
    ///     <para>
    ///         The conversion to a deterministic GUID ensures that the same string always
    ///         maps to the same GUID, allowing for consistent identification of chains
    ///         using human-readable names.
    ///     </para>
    ///     <para>
    ///         In this implementation, no actual asynchronous operations occur since all data
    ///         is stored in memory, but the method maintains the asynchronous signature for
    ///         compatibility with the interface.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     <para>
    ///         This is the core method for storing chain definitions. It adds the provided
    ///         messages to the <see cref="Messages"/> dictionary, using the specified chain ID
    ///         as the key.
    ///     </para>
    ///     <para>
    ///         This implementation uses an additive approach to saving messages:
    ///         <list type="bullet">
    ///             <item>If the chain ID doesn't exist in the dictionary, a new entry is created with the provided messages</item>
    ///             <item>If the chain ID already exists, the provided messages are added to the existing list</item>
    ///         </list>
    ///         This behavior allows for incremental addition of handlers to a chain without
    ///         replacing existing definitions.
    ///     </para>
    ///     <para>
    ///         The method uses <see cref="ConcurrentDictionary{TKey, TValue}.AddOrUpdate"/>
    ///         to ensure thread-safety when updating the dictionary. This allows multiple
    ///         threads to save messages concurrently without conflicts.
    ///     </para>
    ///     <para>
    ///         The method respects the provided cancellation token, returning a failure result
    ///         if cancellation is requested before the operation completes.
    ///     </para>
    ///     <para>
    ///         In this implementation, no actual asynchronous operations occur since all data
    ///         is stored in memory, but the method maintains the asynchronous signature for
    ///         compatibility with the interface.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     <para>
    ///         This method saves the provided execution logs to the <see cref="ExecutionLogs"/>
    ///         dictionary, using the ChainId property of each log as the key.
    ///     </para>
    ///     <para>
    ///         For each log, the method:
    ///         <list type="number">
    ///             <item>Ensures a list exists for the log's ChainId</item>
    ///             <item>Retrieves that list</item>
    ///             <item>Adds the log to the list</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This implementation uses <see cref="ConcurrentDictionary{TKey, TValue}.TryAdd"/>
    ///         to ensure thread-safety when creating new lists for chain IDs. Once a list exists,
    ///         adding to it is also thread-safe thanks to the concurrent dictionary's behavior.
    ///     </para>
    ///     <para>
    ///         The method respects the provided cancellation token, returning a failure result
    ///         if cancellation is requested before the operation completes.
    ///     </para>
    ///     <para>
    ///         In this implementation, no actual asynchronous operations occur since all data
    ///         is stored in memory, but the method maintains the asynchronous signature for
    ///         compatibility with the interface.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     <para>
    ///         This method updates the <see cref="ChainExecutionLog.Status"/> property of the
    ///         provided log to the specified value.
    ///     </para>
    ///     <para>
    ///         In this in-memory implementation, the update is performed directly on the
    ///         log object, taking advantage of the fact that it's a reference type. There's
    ///         no need to locate the log in the dictionary or perform any storage operations
    ///         since the object reference is already stored and shared.
    ///     </para>
    ///     <para>
    ///         This implementation is much simpler than what would be required in a database-backed
    ///         repository, where the log would need to be located in the database and updated
    ///         with a SQL command or equivalent.
    ///     </para>
    ///     <para>
    ///         The cancellation token is not used in this implementation since the operation
    ///         is effectively instantaneous, but it's included for compatibility with the interface.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     <para>
    ///         This method updates the provided log with new status and context data:
    ///         <list type="bullet">
    ///             <item>The <see cref="ChainExecutionLog.Status"/> property is set to the specified value</item>
    ///             <item>The <see cref="ChainExecutionLog.BeforeJson"/> property is set to the specified value</item>
    ///             <item>The <see cref="ChainExecutionLog.AfterJson"/> property is set to the specified value</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         As with <see cref="UpdateChainExecutionLog(ChainExecutionLog, ChainMessageStatus, CancellationToken)"/>,
    ///         this implementation takes advantage of reference sharing to perform the update
    ///         directly on the object, without needing to locate it in storage.
    ///     </para>
    ///     <para>
    ///         This method is typically used when a handler implements <see cref="ISaveBeforeContextData"/>
    ///         and/or <see cref="ISaveAfterContextData"/> to track context state changes.
    ///     </para>
    ///     <para>
    ///         The cancellation token is not used in this implementation since the operation
    ///         is effectively instantaneous, but it's included for compatibility with the interface.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     <para>
    ///         This method retrieves all execution logs associated with the specified chain ID
    ///         from the <see cref="ExecutionLogs"/> dictionary.
    ///     </para>
    ///     <para>
    ///         If the chain ID is not found in the dictionary, an empty list is returned.
    ///         This behavior matches the interface contract, where the absence of execution
    ///         logs is not considered an error condition.
    ///     </para>
    ///     <para>
    ///         This implementation uses <see cref="ConcurrentDictionary{TKey, TValue}.TryGetValue"/>
    ///         to safely attempt to retrieve the logs list, returning an empty list if the key
    ///         is not found.
    ///     </para>
    ///     <para>
    ///         The cancellation token is not used in this implementation since the operation
    ///         is effectively instantaneous, but it's included for compatibility with the interface.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     <para>
    ///         This method simply delegates to <see cref="GetChainExecutionLogs(Guid, CancellationToken)"/>
    ///         with <see cref="DefaultChainGuid"/> as the chain ID. It provides a convenient
    ///         way to access the execution logs for the default chain without needing to know
    ///         its specific GUID.
    ///     </para>
    ///     <para>
    ///         In this implementation, no actual asynchronous operations occur since all data
    ///         is stored in memory, but the method maintains the asynchronous signature for
    ///         compatibility with the interface.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     <para>
    ///         This method is a no-op in the in-memory implementation, since the logs are
    ///         already stored as direct object references. Any changes made to the log objects
    ///         elsewhere in the code are automatically reflected in the stored logs.
    ///     </para>
    ///     <para>
    ///         This is a key difference from database-backed implementations, where updates
    ///         would need to be explicitly persisted to the database. In those implementations,
    ///         this method would typically perform a bulk update operation.
    ///     </para>
    ///     <para>
    ///         The method always returns a success result, since there's nothing that can
    ///         fail in this implementation.
    ///     </para>
    ///     <para>
    ///         The cancellation token is not used in this implementation since the operation
    ///         is effectively instantaneous, but it's included for compatibility with the interface.
    ///     </para>
    /// </remarks>
    public Task<Result> UpdateChainExecutionLog(IEnumerable<ChainExecutionLog> chainExecutionLogs, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Success());
    }
}