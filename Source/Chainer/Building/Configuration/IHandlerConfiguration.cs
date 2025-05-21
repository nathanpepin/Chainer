namespace Chainer.Building.Configuration;

/// <summary>
/// Defines the contract for accessing and manipulating chain handler configuration data.
/// </summary>
/// <remarks>
/// This interface abstracts the storage and conversion of configuration data from various sources.
/// It provides a unified way to bind raw configuration data (from JSON, XML, dictionaries, etc.)
/// to strongly-typed configuration objects used by chain handlers.
/// 
/// It enables handlers to access their configuration without needing to know the underlying
/// format or storage mechanism. This allows for flexible configuration sources while
/// maintaining a clean separation between configuration access and handler logic.
/// 
/// The primary implementation of this interface is ChainHandlerConfiguration, which
/// internally manages different storage formats and performs the necessary conversions.
/// 
/// This interface is used by:
/// <list type="bullet">
///   <item>IConfigurableChainHandler implementations to receive their configuration</item>
///   <item>DynamicChainExecutor to provide configuration to handlers</item>
///   <item>ConfigurableHandler&lt;TContext, TConfig&gt; for automatic configuration binding</item>
/// </list>
/// </remarks>
public interface IHandlerConfiguration
{
    /// <summary>
    /// Gets the current format type of the configuration data.
    /// </summary>
    /// <remarks>
    /// This property indicates how the configuration data is stored internally
    /// and which parsing method will be used during binding operations.
    /// 
    /// A value of NotSet indicates that there is no valid configuration data available.
    /// </remarks>
    HandlerConfigurationType ConfigurationType { get; }

    /// <summary>
    /// Sets or updates the configuration data and its format type.
    /// </summary>
    /// <param name="value">The configuration data to store</param>
    /// <param name="type">The format type of the configuration data</param>
    /// <remarks>
    /// This method allows changing the configuration data at runtime.
    /// It is typically called by the chain execution system when setting up handlers,
    /// but can also be used to update configuration dynamically if needed.
    /// 
    /// Implementations should validate that the value is compatible with the specified type
    /// and throw appropriate exceptions if not.
    /// </remarks>
    void SetConfiguration(object value, HandlerConfigurationType type);

    /// <summary>
    /// Binds the configuration data to a new instance of type T.
    /// </summary>
    /// <typeparam name="T">The configuration class type to bind to</typeparam>
    /// <returns>
    /// An instance of T with properties populated from the configuration data,
    /// or a new default instance if binding fails
    /// </returns>
    /// <remarks>
    /// This method always returns an object, even if binding fails.
    /// The returned object will be a new default instance of T if binding was not successful.
    /// 
    /// This is the simplest way to access typed configuration, but does not provide
    /// feedback on whether binding succeeded. Use TryBind if you need to know that.
    /// 
    /// The type T must have a parameterless constructor and be a reference type.
    /// </remarks>
    T? Bind<T>() where T : class, new();

    /// <summary>
    /// Attempts to bind the configuration data to a new instance of type T.
    /// </summary>
    /// <typeparam name="T">The configuration class type to bind to</typeparam>
    /// <param name="result">When this method returns, contains the bound instance of T if binding succeeded, or null if binding failed</param>
    /// <returns>True if binding succeeded, false otherwise</returns>
    /// <remarks>
    /// This method provides more control than Bind&lt;T&gt; by indicating whether binding
    /// was successful and allowing null to be returned when it fails.
    /// 
    /// Implementations should bind configuration data to a new instance of T according to
    /// the current ConfigurationType. The binding process should map values from the
    /// configuration data to matching properties in type T.
    /// 
    /// If binding fails (due to missing configuration, type mismatch, etc.), the method
    /// should return false and set result to null.
    /// 
    /// The type T must have a parameterless constructor and be a reference type.
    /// </remarks>
    bool TryBind<T>(out T? result) where T : class, new();
}