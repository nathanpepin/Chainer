namespace Chainer.Building.Configuration;

/// <summary>
///     Defines the contract for accessing and manipulating chain handler configuration data.
/// </summary>
public interface IHandlerConfiguration
{
    /// <summary>
    ///     Gets the current format type of the configuration data.
    /// </summary>
    /// <remarks>
    ///     This property indicates how the configuration data is stored internally
    ///     and which parsing method will be used during binding operations.
    ///     A value of NotSet indicates that there is no valid configuration data available.
    /// </remarks>
    HandlerConfigurationType ConfigurationType { get; }

    /// <summary>
    ///     Sets or updates the configuration data and its format type.
    /// </summary>
    void SetConfiguration(object value, HandlerConfigurationType type);

    /// <summary>
    ///     Binds the configuration data to a new instance of type T.
    /// </summary>
    T? Bind<T>() where T : class, new();

    /// <summary>
    ///     Attempts to bind the configuration data to a new instance of type T.
    /// </summary>
    bool TryBind<T>(out T? result) where T : class, new();
}