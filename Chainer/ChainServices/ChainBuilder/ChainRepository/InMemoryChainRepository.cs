using System.Collections.Concurrent;
using Chainer.ChainServices.ChainBuilder.Messages;
using Chainer.ChainServices.Hashing;

namespace Chainer.ChainServices.ChainBuilder.ChainRepository;

public sealed class InMemoryChainRepository : IChainRepository
{
    private Guid DefaultChain = Guid.NewGuid();

    public ConcurrentDictionary<Guid, List<ChainMessage>> Messages { get; } = new();
    public ConcurrentDictionary<Guid, List<ChainExecutionLog>> ExecutionLogs { get; } = new();

    public Task<Result<List<ChainMessage>>> GetDefaultChainMessagesAsync(CancellationToken cancellationToken = default)
    {
        return GetChainMessagesAsync(DefaultChain, cancellationToken);
    }

    public Task<Result<List<ChainMessage>>> GetChainMessagesAsync(string chainId, CancellationToken cancellationToken = default)
    {
        var id = GuidFromString.CreateDeterministicGuid(chainId);
        return GetChainMessagesAsync(id, cancellationToken);
    }

    public Task<Result<List<ChainMessage>>> GetChainMessagesAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(Result.Failure<List<ChainMessage>>("Operation was canceled"));

        if (Messages.TryGetValue(chainId, out var messages))
        {
            return Task.FromResult(Result.Success(messages));
        }

        return Task.FromResult(Result.Success(new List<ChainMessage>()));
    }

    public Task<Result> SaveToDefaultChainMessagesAsync(IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default)
    {
        return SaveChainMessagesAsync(DefaultChain, messages, cancellationToken);
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

    // Since it is in memory and the ChainExecutionLog is a class, no need to update anything
    public Task<Result> UpdateChainExecutionLog(
        ChainExecutionLog chainExecutionLog,
        ChainMessageStatus status,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Success());
    }


    // Since it is in memory and the ChainExecutionLog is a class, no need to update anything
    public Task<Result> UpdateChainExecutionLog(
        ChainExecutionLog chainExecutionLog,
        ChainMessageStatus status,
        string? beforeExecution,
        string afterExecution,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Success());
    }

    // Since it is in memory and the ChainExecutionLog is a class, no need to update anything
    public Task<Result> UpdateChainExecutionLog(IEnumerable<ChainExecutionLog> chainExecutionLogs, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Success());
    }
}