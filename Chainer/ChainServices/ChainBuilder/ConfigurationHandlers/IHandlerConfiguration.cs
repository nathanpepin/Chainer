namespace Chainer.ChainServices.ChainBuilder.ConfigurationHandlers;

/// <summary>
///     Configuration interface specifically for chain handlers
/// </summary>
public interface IHandlerConfiguration
{
    /// <summary>
    ///     Gets a configuration value converted to the specified type
    /// </summary>
    T? GetValue<T>(string key, T? defaultValue = default);

    /// <summary>
    ///     Gets a configuration section
    /// </summary>
    IHandlerConfiguration GetSection(string key);

    /// <summary>
    ///     Binds the configuration to a new instance of T
    /// </summary>
    T Bind<T>() where T : class, new();

    /// <summary>
    ///     Binds the configuration to an existing instance of T
    /// </summary>
    void Bind<T>(T instance) where T : class;

    /// <summary>
    ///     Determines if the configuration contains a setting with the specified key
    /// </summary>
    bool Contains(string key);
}