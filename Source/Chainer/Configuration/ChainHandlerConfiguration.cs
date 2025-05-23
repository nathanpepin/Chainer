using Chainer.Abstractions;

namespace Chainer.Configuration;

/// <summary>
///     Manages handler configuration data in multiple formats (Object, JSON, Dictionary, XML) for chain handlers.
/// </summary>
/// <remarks>
///     This class provides a unified way to store, access, and transform configuration data regardless of its
///     original format. It's primarily used in two scenarios:
///     <list type="bullet">
///         <item>When reading configuration data from external sources like appsettings.json</item>
///         <item>When binding configuration data to handlers in the DynamicChainExecutor</item>
///     </list>
///     The class handles type conversions and binding operations to convert raw configuration data into
///     strongly-typed configuration objects for handlers.
/// </remarks>
public sealed class ChainHandlerConfiguration : IHandlerConfiguration
{
    private IDictionary<string, string?>? _dictionaryData;
    private string? _jsonData;
    private object? _objectData;
    private string? _xmlData;

    /// <summary>
    ///     Creates a new instance of ChainHandlerConfiguration with the specified value and configuration type.
    /// </summary>
    /// <param name="value">The configuration data object</param>
    /// <param name="type">The format type of the configuration data</param>
    /// <exception cref="ArgumentException">Thrown when the value's type doesn't match the specified configuration type</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported configuration type is specified</exception>
    public ChainHandlerConfiguration(object value, HandlerConfigurationType type)
    {
        SetConfiguration(value, type);
    }

    /// <summary>
    ///     Gets the current format type of the configuration data.
    /// </summary>
    public HandlerConfigurationType ConfigurationType { get; private set; } = HandlerConfigurationType.NotSet;

    /// <summary>
    ///     Sets or updates the configuration data and its format type.
    /// </summary>
    /// <param name="value">The configuration data to store</param>
    /// <param name="type">The format type of the configuration data</param>
    /// <exception cref="ArgumentException">Thrown when the value's type doesn't match the specified configuration type</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported configuration type is specified</exception>
    public void SetConfiguration(object? value, HandlerConfigurationType type)
    {
        ResetAllDataFields();

        if (value == null)
        {
            ConfigurationType = HandlerConfigurationType.NotSet;
            return;
        }

        ConfigurationType = type;

        switch (type)
        {
            case HandlerConfigurationType.Object:
                _objectData = value;
                break;
            case HandlerConfigurationType.Json:
                if (value is string json)
                    _jsonData = json;
                else
                    throw new ArgumentException("Value must be a string when using Json configuration type", nameof(value));

                break;
            case HandlerConfigurationType.Dictionary:
                if (value is IDictionary<string, string?> dictionary)
                    _dictionaryData = dictionary;
                else
                    throw new ArgumentException("Value must be an IDictionary<string, string?> when using Dictionary configuration type", nameof(value));

                break;
            case HandlerConfigurationType.Xml:
                if (value is string xml)
                    _xmlData = xml;
                else
                    throw new ArgumentException("Value must be a string when using Xml configuration type", nameof(value));

                break;
            case HandlerConfigurationType.NotSet:
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported configuration type");
        }
    }

    /// <summary>
    ///     Binds the configuration data to a new instance of type T.
    /// </summary>
    /// <typeparam name="T">The configuration class type to bind to</typeparam>
    /// <returns>
    ///     An instance of T with properties populated from the configuration data,
    ///     or a new default instance if binding fails
    /// </returns>
    public T? Bind<T>() where T : class, new()
    {
        return TryBind<T>(out var result)
            ? result
            : new T(); // Return a default instance 
    }

    /// <summary>
    ///     Attempts to bind the configuration data to a new instance of type T.
    /// </summary>
    /// <typeparam name="T">The configuration class type to bind to</typeparam>
    /// <param name="result">
    ///     When this method returns, contains the bound instance of T if binding succeeded, or null if
    ///     binding failed
    /// </param>
    /// <returns>True if binding succeeded, false otherwise</returns>
    /// <exception cref="NotImplementedException">Thrown if ConfigurationType is NotSet or an unhandled value</exception>
    public bool TryBind<T>(out T? result) where T : class, new()
    {
        result = null;

        switch (ConfigurationType)
        {
            case HandlerConfigurationType.Object:
                return ConfigurationParser.ParseObject(_objectData, out result);
            case HandlerConfigurationType.Json:
                return ConfigurationParser.ParseJson(_jsonData, out result);
            case HandlerConfigurationType.Dictionary:
                return ConfigurationParser.ParseDictionary(_dictionaryData, out result);
            case HandlerConfigurationType.Xml:
                return ConfigurationParser.ParseXml(_xmlData, out result);
            case HandlerConfigurationType.NotSet:
            default: throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Clears all internal data fields to prevent inconsistent state.
    /// </summary>
    private void ResetAllDataFields()
    {
        _dictionaryData = null;
        _jsonData = null;
        _objectData = null;
        _xmlData = null;
    }

    /// <summary>
    ///     Creates a handler configuration from a raw object.
    /// </summary>
    /// <param name="value">The object to use as configuration data</param>
    /// <returns>A new IHandlerConfiguration instance with Object format type</returns>
    public static IHandlerConfiguration FromObject(object value)
    {
        return new ChainHandlerConfiguration(value, HandlerConfigurationType.Object);
    }

    /// <summary>
    ///     Creates a handler configuration from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to use as configuration data</param>
    /// <returns>A new IHandlerConfiguration instance with JSON format type</returns>
    public static IHandlerConfiguration FromJson(string json)
    {
        return new ChainHandlerConfiguration(json, HandlerConfigurationType.Json);
    }

    /// <summary>
    ///     Creates a handler configuration from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to use as configuration data</param>
    /// <returns>A new IHandlerConfiguration instance with Dictionary format type</returns>
    public static IHandlerConfiguration FromDictionary(Dictionary<string, object?> dictionary)
    {
        return new ChainHandlerConfiguration(dictionary, HandlerConfigurationType.Dictionary);
    }

    /// <summary>
    ///     Creates a handler configuration from an XML string.
    /// </summary>
    /// <param name="xml">The XML string to use as configuration data</param>
    /// <returns>A new IHandlerConfiguration instance with XML format type</returns>
    public static IHandlerConfiguration FromXml(string xml)
    {
        return new ChainHandlerConfiguration(xml, HandlerConfigurationType.Xml);
    }
}