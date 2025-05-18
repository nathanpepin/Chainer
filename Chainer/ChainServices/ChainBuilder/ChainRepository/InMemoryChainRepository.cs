using System.Collections.Concurrent;
using Chainer.ChainServices.ChainBuilder.Messages;
using Chainer.ChainServices.Hashing;

namespace Chainer.ChainServices.ChainBuilder.ChainRepository;

public sealed class InMemoryChainRepository : IChainRepository
{
    public static readonly Guid DefaultChainGuid = new("6ae8a81e-d7f0-43d2-9617-dfd4528b0c89");

    public ConcurrentDictionary<Guid, List<ChainMessage>> Messages { get; } = new();
    public ConcurrentDictionary<Guid, List<ChainExecutionLog>> ExecutionLogs { get; } = new();

    public Task<Result<List<ChainMessage>>> GetDefaultChainMessagesAsync(CancellationToken cancellationToken = default)
    {
        return GetChainMessagesAsync(DefaultChainGuid, cancellationToken);
    }

    public Task<Result<List<ChainMessage>>> GetChainMessagesAsync(string chainId, CancellationToken cancellationToken = default)
    {
        var id = GuidFromString.CreateDeterministicGuid(chainId);
        return GetChainMessagesAsync(id, cancellationToken);
    }

    public Task<Result<List<ChainMessage>>> GetChainMessagesAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(Result<List<ChainMessage>>.Failure("Operation was canceled"));

        if (Messages.TryGetValue(chainId, out var messages))
        {
            return Task.FromResult(Result<List<ChainMessage>>.Success(messages));
        }

        return Task.FromResult(Result<List<ChainMessage>>.Success([]));
    }

    public Task<Result> SaveToDefaultChainMessagesAsync(IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default)
    {
        var chainMessages = messages as ChainMessage[] ?? messages.ToArray();
        foreach (var message in chainMessages)
        {
            message.ChainId = DefaultChainGuid;
        }

        return SaveChainMessagesAsync(DefaultChainGuid, chainMessages, cancellationToken);
    }

    public Task<Result> SaveChainMessagesAsync(string chainId, IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default)
    {
        var id = GuidFromString.CreateDeterministicGuid(chainId);
        return SaveChainMessagesAsync(id, messages, cancellationToken);
    }

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

    public Task<Result> UpdateChainExecutionLog(
        ChainExecutionLog chainExecutionLog,
        ChainMessageStatus status,
        CancellationToken cancellationToken = default)
    {
        chainExecutionLog.Status = status;
        return Task.FromResult(Success());
    }


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

    public Task<List<ChainExecutionLog>> GetChainExecutionLogs(Guid chainId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ExecutionLogs.TryGetValue(chainId, out var logs) ? logs : []);
    }

    public Task<List<ChainExecutionLog>> GetDefaultChainExecutionLogs(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ExecutionLogs.TryGetValue(DefaultChainGuid, out var logs) ? logs : []);
    }

    // Since it is in memory and the ChainExecutionLog is a class, no need to update anything
    public Task<Result> UpdateChainExecutionLog(IEnumerable<ChainExecutionLog> chainExecutionLogs, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Success());
    }
}