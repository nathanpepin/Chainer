using Chainer.ChainServices.ChainBuilder.Messages;

namespace Chainer.ChainServices.ChainBuilder.ChainRepository;

public interface IChainRepository
{
    Task<Result<List<ChainMessage>>> GetDefaultChainMessagesAsync(CancellationToken cancellationToken = default);
    Task<Result<List<ChainMessage>>> GetChainMessagesAsync(string chainId, CancellationToken cancellationToken = default);
    Task<Result<List<ChainMessage>>> GetChainMessagesAsync(Guid chainId, CancellationToken cancellationToken = default);
    Task<Result> SaveToDefaultChainMessagesAsync(IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default);
    Task<Result> SaveChainMessagesAsync(string chainId, IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default);
    Task<Result> SaveChainMessagesAsync(Guid chainId, IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default);
    Task<Result> SaveChainExecutionLogs(IEnumerable<ChainExecutionLog> chainExecutionLogs, CancellationToken cancellationToken = default);

    Task<Result> UpdateChainExecutionLog(
        ChainExecutionLog chainExecutionLog,
        ChainMessageStatus status,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateChainExecutionLog(
        ChainExecutionLog chainExecutionLog,
        ChainMessageStatus status,
        string? beforeExecution,
        string afterExecution,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateChainExecutionLog(IEnumerable<ChainExecutionLog> chainExecutionLogs, CancellationToken cancellationToken = default);
    Task<List<ChainExecutionLog>> GetChainExecutionLogs(Guid chainId, CancellationToken cancellationToken = default);
    Task<List<ChainExecutionLog>> GetDefaultChainExecutionLogs(CancellationToken cancellationToken = default);
}