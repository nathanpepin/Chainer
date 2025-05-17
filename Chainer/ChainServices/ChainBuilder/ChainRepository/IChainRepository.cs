using Chainer.ChainServices.ChainBuilder.Messages;

namespace Chainer.ChainServices.ChainBuilder.ChainRepository;

public interface IChainRepository
{
    Task<Result<List<ChainMessageRecord>>> GetChainMessagesAsync(Guid chainId, CancellationToken cancellationToken = default);
    Task<Result> SaveChainMessagesAsync(Guid chainId, IEnumerable<ChainMessageRecord> messages, CancellationToken cancellationToken = default);
    Task<Result> UpdateChainMessageStatusAsync(Guid messageId, ChainMessageStatus status, string? errorMessage = null, CancellationToken cancellationToken = default);
}