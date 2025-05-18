using Chainer.ChainServices.ChainBuilder.Messages;

namespace Chainer.ChainServices.ChainBuilder.ChainRepository;

public interface IChainRepository
{
    Task<Result<List<ChainMessage>>> GetChainMessagesAsync(Guid chainId, CancellationToken cancellationToken = default);
    Task<Result> SaveChainMessagesAsync(Guid chainId, IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default);
    Task<Result> SaveChainExecutionLogs(IEnumerable<ChainExecutionLog> chainExecutionLog, CancellationToken cancellationToken = default);
    Task<Result> UpdateChainExecutionLog(ChainExecutionLog chainExecutionLog, ChainMessageStatus executing, CancellationToken cancellationToken = default);

    Task<Result> UpdateChainExecutionLog(ChainExecutionLog chainExecutionLog, ChainMessageStatus executing, string? beforeExecution, string afterExecution,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateChainExecutionLog(IEnumerable<ChainExecutionLog> chainExecutionLog, CancellationToken cancellationToken = default);
}