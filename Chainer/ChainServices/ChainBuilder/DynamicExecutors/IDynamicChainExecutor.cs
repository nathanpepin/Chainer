using Chainer.ChainServices.ChainBuilder.Messages;

namespace Chainer.ChainServices.ChainBuilder.DynamicExecutors;

public interface IDynamicChainExecutor
{
    Task<DynamicChainExecutionResult<TContext>> ExecuteDefaultChainAsync<TContext>(
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    Task<DynamicChainExecutionResult<TContext>> ExecuteChainAsync<TContext>(
        string friendlyName,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    Task<DynamicChainExecutionResult<TContext>> ExecuteChainAsync<TContext>(
        Guid chainId,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    Task<DynamicChainExecutionResult<TContext>> ExecuteChainAsync<TContext>(
        IEnumerable<ChainMessage> chainMessages,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();
}