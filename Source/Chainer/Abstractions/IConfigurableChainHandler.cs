namespace Chainer.Abstractions;

/// <summary>
///     Defines a chain handler that can receive and use external configuration.
/// </summary>
public interface IConfigurableChainHandler<TContext> : IChainHandler<TContext>
    where TContext : class, ICloneable, new()
{
    /// <summary>
    ///     Configures the handler with external configuration data.
    /// </summary>
    /// <param name="configuration">The configuration data provider</param>
    /// <remarks>
    ///     This method is called by the chain execution system (typically DynamicChainExecutor)
    ///     before the handler's Handle method is invoked. It provides the handler with access
    ///     to its configuration data, which can be in various formats like JSON, XML,
    ///     dictionary, or a direct object reference.
    ///     Implementations should:
    ///     <list type="bullet">
    ///         <item>Extract needed values from the configuration using Bind&lt;T&gt; or TryBind&lt;T&gt;</item>
    ///         <item>Store configuration values in instance fields for use during Handle</item>
    ///         <item>Handle the case where binding fails or configuration is incomplete</item>
    ///         <item>Validate configuration values if necessary</item>
    ///     </list>
    ///     This method should not throw exceptions under normal circumstances, as that would
    ///     prevent the chain from executing. Instead, it should handle missing or invalid
    ///     configuration gracefully, perhaps by logging warnings and using default values.
    /// </remarks>
    void Configure(IHandlerConfiguration configuration);
}