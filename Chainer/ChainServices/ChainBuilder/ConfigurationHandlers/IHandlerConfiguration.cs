namespace Chainer.ChainServices.ChainBuilder.ConfigurationHandlers;

/// <summary>
///     Configuration interface specifically for chain handlers
/// </summary>
public interface IHandlerConfiguration
{
    HandlerConfigurationType ConfigurationType { get; }
    void SetConfiguration(object value, HandlerConfigurationType type);
    T? Bind<T>() where T : class, new();
    bool TryBind<T>(out T? result) where T : class, new();
}