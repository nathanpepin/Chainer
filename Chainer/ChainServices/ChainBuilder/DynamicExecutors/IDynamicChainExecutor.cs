namespace Chainer.ChainServices.ChainBuilder.DynamicExecutors;

public interface IDynamicChainExecutor
{
    Task<Result<TContext>> ExecuteChainAsync<TContext>(
        Guid chainId,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();
}