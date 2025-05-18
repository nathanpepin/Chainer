using Chainer.ChainServices.ChainBuilder.Messages;

namespace Chainer.ChainServices.ChainBuilder.DynamicExecutors;

public interface IDynamicChainExecutor
{
    Task<Result<TContext>> ExecuteChainAsync<TContext>(
        Guid chainId,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    Task<Result<TContext>> ExecuteChainAsync<TContext>(
        IEnumerable<ChainMessage> chainMessages,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();
}