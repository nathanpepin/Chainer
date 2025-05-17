using System.Text.Json;

namespace Chainer.ChainServices.ChainBuilder.ConfigurationHandlers;

public sealed class HandlerConfiguration : IHandlerConfiguration
{
    private readonly IDictionary<string, object?> _data;
    private readonly string _path;

    // Creates a new root configuration
    public HandlerConfiguration()
    {
        _data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        _path = string.Empty;
    }

    // Creates a section configuration
    private HandlerConfiguration(IDictionary<string, object?> data, string path)
    {
        _data = data;
        _path = path;
    }

    public T? GetValue<T>(string key, T? defaultValue = default)
    {
        if (string.IsNullOrEmpty(key))
            return defaultValue;

        // Check for a direct match in current section
        if (_data.TryGetValue(key, out var value)) return ConvertValue<T>(value) ?? defaultValue;

        // Handle dot notation for nested sections
        var keyParts = key.Split(['.'], 2);
        if (keyParts.Length == 2 && _data.TryGetValue(keyParts[0], out var section))
            if (section is HandlerConfiguration sectionConfig)
                return sectionConfig.GetValue(keyParts[1], defaultValue);

        return defaultValue;
    }

    public IHandlerConfiguration GetSection(string key)
    {
        if (string.IsNullOrEmpty(key))
            return this;

        // Direct section match
        if (_data.TryGetValue(key, out var value) && value is HandlerConfiguration section) return section;

        // Create a section via hierarchical key
        var keyParts = key.Split(['.'], 2);
        if (keyParts.Length != 2 || !_data.TryGetValue(keyParts[0], out var firstSection))
            return new HandlerConfiguration(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase), key);

        if (firstSection is HandlerConfiguration sectionConfig) return sectionConfig.GetSection(keyParts[1]);

        // Return empty configuration for non-existent sections
        return new HandlerConfiguration(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase), key);
    }

    public T Bind<T>() where T : class, new()
    {
        var instance = new T();
        Bind(instance);
        return instance;
    }

    public void Bind<T>(T instance) where T : class
    {
        ArgumentNullException.ThrowIfNull(instance);

        var type = typeof(T);
        var properties = type.GetProperties().Where(p => p.CanWrite);

        foreach (var property in properties)
        {
            var propertyType = property.PropertyType;
            var key = property.Name;

            // Check if a value exists for this property
            if (!_data.TryGetValue(key, out var rawValue)) continue;

            if (rawValue is HandlerConfiguration section && !propertyType.IsPrimitive && propertyType != typeof(string))
            {
                // Handle complex type
                if (!propertyType.IsClass) continue;

                var value = property.GetValue(instance);
                if (value is null)
                {
                    value = Activator.CreateInstance(propertyType);
                    property.SetValue(instance, value);
                }

                if (value is not null) section.Bind(value);
            }
            else
            {
                // Handle simple type
                var value = ConvertValue(rawValue, propertyType);
                if (value != null) property.SetValue(instance, value);
            }
        }
    }

    public bool Contains(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        // Direct match
        if (_data.ContainsKey(key))
            return true;

        // Check nested sections
        var keyParts = key.Split(['.'], 2);
        if (keyParts.Length == 2 && _data.TryGetValue(keyParts[0], out var section))
            if (section is HandlerConfiguration sectionConfig)
                return sectionConfig.Contains(keyParts[1]);

        return false;
    }

    // Create from JSON string
    public static IHandlerConfiguration FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new HandlerConfiguration();

        var config = new HandlerConfiguration();
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        PopulateFromJsonElement(config._data, jsonElement);
        return config;
    }

    // Create from dictionary
    public static IHandlerConfiguration FromDictionary(IDictionary<string, object?> dictionary)
    {
        var config = new HandlerConfiguration();
        foreach (var (key, value) in dictionary)
            if (value is IDictionary<string, object?> dict)
            {
                config._data[key] = FromDictionary(dict);
            }
            else if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    var nestedData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    PopulateFromJsonElement(nestedData, jsonElement);
                    config._data[key] = new HandlerConfiguration(nestedData, key);
                }
                else
                {
                    config._data[key] = ConvertJsonElement(jsonElement);
                }
            }
            else
            {
                config._data[key] = value;
            }

        return config;
    }

    private static void PopulateFromJsonElement(IDictionary<string, object?> data, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in element.EnumerateObject())
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                var nestedData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                PopulateFromJsonElement(nestedData, property.Value);
                data[property.Name] = new HandlerConfiguration(nestedData, property.Name);
            }
            else
            {
                data[property.Name] = ConvertJsonElement(property.Value);
            }
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            _ => element.ToString()
        };
    }

    private static T? ConvertValue<T>(object? value)
    {
        return (T?)ConvertValue(value, typeof(T));
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        if (targetType.IsInstanceOfType(value))
            return value;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null) targetType = underlyingType;

        // Handle numeric conversions
        if ((value is IConvertible || value is JsonElement) &&
            (targetType.IsPrimitive || targetType == typeof(decimal) || targetType == typeof(string)))
            try
            {
                if (value is not JsonElement element) return Convert.ChangeType(value, targetType);

                if (targetType == typeof(bool) && element.ValueKind == JsonValueKind.True)
                    return true;
                if (targetType == typeof(bool) && element.ValueKind == JsonValueKind.False)
                    return false;

                if (targetType == typeof(string))
                    return element.ToString();

                if (element.ValueKind != JsonValueKind.Number) return Convert.ChangeType(value, targetType);

                if (targetType == typeof(int)) return element.GetInt32();
                if (targetType == typeof(long)) return element.GetInt64();
                if (targetType == typeof(double)) return element.GetDouble();
                if (targetType == typeof(decimal)) return element.GetDecimal();

                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                /* Continue with other conversion methods */
            }

        // Handle enum conversions
        if (targetType.IsEnum)
            switch (value)
            {
                case string strValue:
                    return Enum.Parse(targetType, strValue, true);
                case IConvertible numValue:
                    return Enum.ToObject(targetType, Convert.ToInt32(numValue));
            }

        // Handle TimeSpan
        if (targetType == typeof(TimeSpan))
            switch (value)
            {
                case string s when TimeSpan.TryParse(s, out var timeSpan):
                    return timeSpan;
                case double d:
                    return TimeSpan.FromSeconds(d);
            }

        // Last resort - try string conversion
        try
        {
            var stringValue = value.ToString();
            if (stringValue != null && targetType != typeof(object))
                return Convert.ChangeType(stringValue, targetType);
        }
        catch
        {
            /* Failed to convert */
        }

        return null;
    }
}