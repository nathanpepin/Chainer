using Chainer.Core;

namespace Chainer.Building.Configuration;

/// <summary>
/// Defines a chain handler that can receive and use external configuration.
/// </summary>
/// <typeparam name="TContext">The context type that this handler processes</typeparam>
/// <remarks>
/// This interface extends IChainHandler&lt;TContext&gt; to add configuration capabilities.
/// Chain handlers that implement this interface can receive configuration data during 
/// execution setup, allowing them to customize their behavior based on external settings
/// without requiring code changes or recompilation.
/// 
/// Configurable handlers are particularly useful in scenarios where:
/// <list type="bullet">
///   <item>Handler behavior needs to be adjusted per environment (dev, staging, production)</item>
///   <item>Business rules encoded in handlers may change frequently</item>
///   <item>The same handler needs different settings in different chains</item>
///   <item>Configuration values should be externalized in config files rather than hardcoded</item>
/// </list>
/// 
/// The DynamicChainExecutor identifies handlers that implement this interface and automatically
/// calls their Configure method with the appropriate configuration before execution.
/// 
/// For simple configuration scenarios, consider inheriting from ConfigurableHandler&lt;TContext, TConfig&gt;
/// which provides automatic binding to a strongly-typed configuration class.
/// 
/// Example implementation:
/// <code>
/// public class EmailNotificationHandler : IConfigurableChainHandler&lt;OrderContext&gt;
/// {
///     private string _templatePath;
///     private bool _sendCcToSupport;
///     
///     public void Configure(IHandlerConfiguration configuration)
///     {
///         if (configuration.TryBind&lt;EmailConfig&gt;(out var config) &amp;&amp; config != null)
///         {
///             _templatePath = config.TemplatePath;
///             _sendCcToSupport = config.SendCcToSupport;
///         }
///     }
///     
///     public Task&lt;Result&lt;OrderContext&gt;&gt; Handle(
///         OrderContext context,
///         ILogger? logger = null,
///         CancellationToken cancellationToken = default)
///     {
///         // Use _templatePath and _sendCcToSupport in email sending logic
///         // ...
///         return Task.FromResult&lt;Result&lt;OrderContext&gt;&gt;(context);
///     }
/// }
/// </code>
/// </remarks>
public interface IConfigurableChainHandler<TContext> : IChainHandler<TContext>
    where TContext : class, ICloneable, new()
{
    /// <summary>
    /// Configures the handler with external configuration data.
    /// </summary>
    /// <param name="configuration">The configuration data provider</param>
    /// <remarks>
    /// This method is called by the chain execution system (typically DynamicChainExecutor)
    /// before the handler's Handle method is invoked. It provides the handler with access
    /// to its configuration data, which can be in various formats like JSON, XML, 
    /// dictionary, or a direct object reference.
    /// 
    /// Implementations should:
    /// <list type="bullet">
    ///   <item>Extract needed values from the configuration using Bind&lt;T&gt; or TryBind&lt;T&gt;</item>
    ///   <item>Store configuration values in instance fields for use during Handle</item>
    ///   <item>Handle the case where binding fails or configuration is incomplete</item>
    ///   <item>Validate configuration values if necessary</item>
    /// </list>
    /// 
    /// This method should not throw exceptions under normal circumstances, as that would
    /// prevent the chain from executing. Instead, it should handle missing or invalid
    /// configuration gracefully, perhaps by logging warnings and using default values.
    /// </remarks>
    void Configure(IHandlerConfiguration configuration);
}