using System.Collections;
using System.Text.Json;
using System.Xml.Serialization;

namespace Chainer.ChainServices.ChainBuilder.ConfigurationHandlers;

public sealed class HandlerConfiguration : IHandlerConfiguration
{
    public HandlerConfigurationType ConfigurationType { get; private set; } = HandlerConfigurationType.NotSet;

    private IDictionary<string, object?>? _dictionaryData;
    private string? _jsonData;
    private object? _objectData;
    private string? _xmlData;

    public HandlerConfiguration(object value, HandlerConfigurationType type)
    {
        SetConfiguration(value, type);
    }

    public static IHandlerConfiguration FromObject(object value)
    {
        return new HandlerConfiguration(value, HandlerConfigurationType.Object);
    }

    public static IHandlerConfiguration FromJson(string json)
    {
        return new HandlerConfiguration(json, HandlerConfigurationType.Json);
    }

    public static IHandlerConfiguration FromDictionary(IDictionary<string, object?> dictionary)
    {
        return new HandlerConfiguration(dictionary, HandlerConfigurationType.Dictionary);
    }

    public static IHandlerConfiguration FromXml(string xml)
    {
        return new HandlerConfiguration(xml, HandlerConfigurationType.Xml);
    }

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
                if (value is IDictionary<string, object?> dictionary)
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
        if (TryBind<T>(out var result))
        {
            return result;
        }

        return new T(); // Return a default instance if binding fails
    }

    public bool TryBind<T>(out T? result) where T : class, new()
    {
        result = null;

        try
        {
            switch (ConfigurationType)
            {
                case HandlerConfigurationType.Object:
                    if (_objectData is T typedObject)
                    {
                        result = typedObject;
                        return true;
                    }
                    else if (_objectData != null)
                    {
                        // Try to convert via JSON serialization/deserialization
                        var json = JsonSerializer.Serialize(_objectData);
                        result = JsonSerializer.Deserialize<T>(json);
                        return result != null;
                    }

                    break;

                case HandlerConfigurationType.Json:
                    if (!string.IsNullOrEmpty(_jsonData))
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        result = JsonSerializer.Deserialize<T>(_jsonData, options);
                        return result != null;
                    }

                    break;

                case HandlerConfigurationType.Dictionary:
                    if (_dictionaryData != null)
                    {
                        result = DictionaryToObject<T>(_dictionaryData);
                        return true;
                    }

                    break;

                case HandlerConfigurationType.Xml:
                    if (!string.IsNullOrEmpty(_xmlData))
                    {
                        var serializer = new XmlSerializer(typeof(T));
                        using var reader = new StringReader(_xmlData);
                        var obj = serializer.Deserialize(reader);
                        if (obj is T typedXmlObject)
                        {
                            result = typedXmlObject;
                            return true;
                        }
                    }

                    break;
                case HandlerConfigurationType.NotSet:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private T DictionaryToObject<T>(IDictionary<string, object?> dictionary) where T : class, new()
    {
        var instance = new T();
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in dictionary)
        {
            if (!properties.TryGetValue(entry.Key, out var property)) continue;

            try
            {
                var value = ConvertValue(entry.Value, property.PropertyType);
                if (value != null || Nullable.GetUnderlyingType(property.PropertyType) != null)
                {
                    property.SetValue(instance, value);
                }
            }
            catch
            {
                // Skip properties that can't be converted
            }
        }

        return instance;
    }

    private object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
        {
            return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
                ? Activator.CreateInstance(targetType)
                : null;
        }

        // If value is already the target type, return it
        if (targetType.IsInstanceOfType(value))
            return value;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
        {
            targetType = underlyingType;
        }

        switch (value)
        {
            // Handle complex objects
            case IDictionary<string, object?> nestedDict when
                !targetType.IsPrimitive && targetType != typeof(string) &&
                !targetType.IsEnum && Activator.CreateInstance(targetType) is { } nestedObj:
            {
                var nestedProps = targetType.GetProperties()
                    .Where(p => p.CanWrite)
                    .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

                foreach (var entry in nestedDict)
                {
                    if (!nestedProps.TryGetValue(entry.Key, out var property)) continue;

                    try
                    {
                        var convertedValue = ConvertValue(entry.Value, property.PropertyType);
                        if (convertedValue != null || Nullable.GetUnderlyingType(property.PropertyType) != null)
                        {
                            property.SetValue(nestedObj, convertedValue);
                        }
                    }
                    catch
                    {
                        // Skip properties that can't be converted
                    }
                }

                return nestedObj;
            }
            // Handle collections/lists
            case IList listValue when typeof(IList).IsAssignableFrom(targetType) &&
                                      targetType.IsGenericType && Activator.CreateInstance(targetType) is IList targetList:
            {
                var elementType = targetType.GetGenericArguments()[0];

                foreach (var item in listValue)
                {
                    var convertedItem = ConvertValue(item, elementType);
                    if (convertedItem != null || Nullable.GetUnderlyingType(elementType) == null)
                    {
                        targetList.Add(convertedItem);
                    }
                }

                return targetList;
            }
            // Handle JsonElement to primitive/enum conversion
            case JsonElement element:
                return element.ValueKind switch
                {
                    JsonValueKind.String => ConvertValue(element.GetString(), targetType),
                    JsonValueKind.Number => element.TryGetInt64(out var l) ? ConvertValue(l, targetType) : ConvertValue(element.GetDouble(), targetType),
                    JsonValueKind.True => ConvertValue(true, targetType),
                    JsonValueKind.False => ConvertValue(false, targetType),
                    _ => null
                };
        }

        // Handle primitive types and enums
        try
        {
            // Handle enum conversions
            if (targetType.IsEnum)
            {
                switch (value)
                {
                    case string strValue:
                        return Enum.Parse(targetType, strValue, ignoreCase: true);
                    case IConvertible numValue:
                        return Enum.ToObject(targetType, Convert.ToInt32(numValue));
                }
            }

            // Handle TimeSpan
            if (targetType == typeof(TimeSpan))
            {
                if (value is string s && TimeSpan.TryParse(s, out var timeSpan))
                    return timeSpan;
                if (value is double d)
                    return TimeSpan.FromSeconds(d);
            }

            // Handle DateTime
            if (targetType == typeof(DateTime))
            {
                if (value is string s && DateTime.TryParse(s, out var dateTime))
                    return dateTime;
            }

            // Try standard conversion
            if (value is IConvertible)
            {
                return Convert.ChangeType(value, targetType);
            }
        }
        catch
        {
            // Conversion failed, try string-based approach
        }

        // Last-ditch effort: try string conversion
        try
        {
            var stringValue = value.ToString();
            if (stringValue != null)
            {
                return Convert.ChangeType(stringValue, targetType);
            }
        }
        catch
        {
            // Last resort failed
        }

        return null;
    }
}