using System.Collections;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;

namespace Chainer.Building.Configuration;

public sealed class ChainHandlerConfiguration : IHandlerConfiguration
{
    private IDictionary<string, string?>? _dictionaryData;
    private string? _jsonData;
    private object? _objectData;
    private string? _xmlData;

    public ChainHandlerConfiguration(object value, HandlerConfigurationType type)
    {
        SetConfiguration(value, type);
    }

    public HandlerConfigurationType ConfigurationType { get; private set; } = HandlerConfigurationType.NotSet;

    public void SetConfiguration(object? value, HandlerConfigurationType type)
    {
        if (value == null)
        {
            ConfigurationType = HandlerConfigurationType.NotSet;
            _dictionaryData = null;
            _jsonData = null;
            _objectData = null;
            _xmlData = null;
            return;
        }

        ConfigurationType = type;

        switch (type)
        {
            case HandlerConfigurationType.Object:
                _objectData = value;
                _dictionaryData = null;
                _jsonData = null;
                _xmlData = null;
                break;

            case HandlerConfigurationType.Json:
                if (value is string json)
                {
                    _jsonData = json;
                    _objectData = null;
                    _dictionaryData = null;
                    _xmlData = null;
                }
                else
                {
                    throw new ArgumentException("Value must be a string when using Json configuration type", nameof(value));
                }

                break;

            case HandlerConfigurationType.Dictionary:
                if (value is IDictionary<string, string?> dictionary)
                {
                    _dictionaryData = dictionary;
                    _objectData = null;
                    _jsonData = null;
                    _xmlData = null;
                }
                else
                {
                    throw new ArgumentException("Value must be an IDictionary<string, object?> when using Dictionary configuration type", nameof(value));
                }

                break;

            case HandlerConfigurationType.Xml:
                if (value is string xml)
                {
                    _xmlData = xml;
                    _objectData = null;
                    _dictionaryData = null;
                    _jsonData = null;
                }
                else
                {
                    throw new ArgumentException("Value must be a string when using Xml configuration type", nameof(value));
                }

                break;

            case HandlerConfigurationType.NotSet:
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported configuration type");
        }
    }

    public T? Bind<T>() where T : class, new()
    {
        return TryBind<T>(out var result)
            ? result
            : new T(); // Return a default instance if binding fails
    }

    public bool TryBind<T>(out T? result) where T : class, new()
    {
        result = null;

        switch (ConfigurationType)
        {
            case HandlerConfigurationType.Object:
                return ConfigurationExtensions.ParseObject(_objectData, out result);
            case HandlerConfigurationType.Json:
                return ConfigurationExtensions.ParseJson(_jsonData, out result);
            case HandlerConfigurationType.Dictionary:
                return ConfigurationExtensions.ParseDictionary(_dictionaryData, out result);
            case HandlerConfigurationType.Xml:
                return ConfigurationExtensions.ParseXml(_xmlData, out result);
            case HandlerConfigurationType.NotSet:
            default: throw new NotImplementedException();
        }
    }

    public static IHandlerConfiguration FromObject(object value)
    {
        return new ChainHandlerConfiguration(value, HandlerConfigurationType.Object);
    }

    public static IHandlerConfiguration FromJson(string json)
    {
        return new ChainHandlerConfiguration(json, HandlerConfigurationType.Json);
    }

    public static IHandlerConfiguration FromDictionary(Dictionary<string, object?> dictionary)
    {
        return new ChainHandlerConfiguration(dictionary, HandlerConfigurationType.Dictionary);
    }

    public static IHandlerConfiguration FromXml(string xml)
    {
        return new ChainHandlerConfiguration(xml, HandlerConfigurationType.Xml);
    }
}