using Chainer.Abstractions;
using Microsoft.Extensions.Logging;

namespace Chainer.Configuration;

/// <summary>
///     Abstract base class for creating chain handlers that require configuration of type TConfig.
/// </summary>
/// <typeparam name="TContext">The context type that this handler will process</typeparam>
/// <typeparam name="TConfig">The configuration type that this handler requires</typeparam>
/// <remarks>
///     This class simplifies the implementation of configurable handlers by providing automatic
///     binding of configuration data to a strongly-typed TConfig object. Subclasses only need to
///     implement the Handle method and can access the bound configuration through the Configuration property.
///     The class implements IConfigurableChainHandler&lt;TContext&gt;, making it compatible with
///     the DynamicChainExecutor's configuration injection mechanism.
///     Example usage:
///     <code>
/// public class DiscountHandler : ConfigurableHandler&lt;PriceContext, DiscountConfig&gt;
/// {
///     public override Task&lt;Result&lt;PriceContext&gt;&gt; Handle(
///         PriceContext context, 
///         ILogger? logger = null, 
///         CancellationToken cancellationToken = default)
///     {
///         // Access configuration using Configuration property
///         decimal discountAmount = Configuration?.Amount ?? 0;
///         context.Price -= discountAmount;
///         return Task.FromResult&lt;Result&lt;PriceContext&gt;&gt;(context);
///     }
/// }
/// 
/// public class DiscountConfig
/// {
///     public decimal Amount { get; set; }
///     public bool ApplyToAll { get; set; }
/// }
/// </code>
/// </remarks>
public abstract class HandlerConfiguration<TContext, TConfig> : IConfigurableChainHandler<TContext>
    where TContext : class, ICloneable, new()
    where TConfig : class, new()
{
    /// <summary>
    ///     Gets the current configuration for this handler.
    /// </summary>
    protected TConfig? Configuration { get; private set; } = new();

    /// <summary>
    ///     Configures the handler by binding the provided configuration data to a TConfig object.
    /// </summary>
    /// <param name="configuration">The configuration data provider</param>
    public virtual void Configure(IHandlerConfiguration configuration)
    {
        Configuration = configuration.Bind<TConfig>();
    }

    /// <summary>
    ///     Processes the context and returns a Result containing the modified context or an error.
    /// </summary>
    /// <param name="context">The context to process</param>
    /// <param name="logger">Optional logger for diagnostic output</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A Task containing a Result with either the processed context or an error</returns>
    public abstract Task<Result<TContext>> Handle(
        TContext context,
        ILogger? logger = null,
        CancellationToken cancellationToken = default);
}