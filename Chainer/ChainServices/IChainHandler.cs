using Microsoft.Extensions.Logging;

namespace Chainer.ChainServices;

/// <summary>
///     An execution over the current context that modifies the context.
///     Can be used for logging, validation, importing, exporting, or any other operation that needs to be done on the context.
/// </summary>
/// <typeparam name="TContext">The context to be modified in the chain</typeparam>
public interface IChainHandler<TContext> where TContext : class, ICloneable, new()
{
    Task<Result<TContext>> Handle(TContext context, ILogger? logger = null, CancellationToken cancellationToken = default);
}