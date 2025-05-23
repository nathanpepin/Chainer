using System.Text;
using Microsoft.Extensions.Configuration;

namespace Chainer.Configuration;

/// <summary>
///     Provides extension methods for parsing various data formats into strongly-typed configuration objects.
/// </summary>
/// <remarks>
///     This utility class supports the ChainHandlerConfiguration implementation by enabling
///     the conversion of different configuration formats (Object, JSON, Dictionary, XML) into
///     typed objects. It leverages the Microsoft.Extensions.Configuration system for most of the parsing.
/// </remarks>
public static class ConfigurationParser
{
    /// <summary>
    ///     Attempts to parse an object directly as type T.
    /// </summary>
    public static bool ParseObject<T>(object? objectData, out T? result) where T : class, new()
    {
        if (objectData is not T typedObject)
        {
            result = null;
            return false;
        }

        result = typedObject;
        return true;
    }

    /// <summary>
    ///     Attempts to parse a JSON string into an object of type T.
    /// </summary>
    public static bool ParseJson<T>(string? jsonText, out T? result) where T : class, new()
    {
        if (string.IsNullOrWhiteSpace(jsonText) || jsonText.Trim(' ').Trim('\r').Trim('\n') == "{}")
        {
            result = null;
            return false;
        }

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(jsonText)));
        var configuration = configurationBuilder.Build();

        result = configuration.Get<T>();
        return result is not null;
    }

    /// <summary>
    ///     Attempts to parse a Dictionary&lt;string, string?&gt; into an object of type T.
    /// </summary>
    public static bool ParseDictionary<T>(Dictionary<string, string?>? dictionaryData, out T? result) where T : class, new()
    {
        if (dictionaryData is null)
        {
            result = null;
            return false;
        }

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(dictionaryData);
        var configuration = configurationBuilder.Build();

        result = configuration.Get<T>();
        return result is not null;
    }

    /// <summary>
    ///     Attempts to parse an IDictionary&lt;string, string?&gt; into an object of type T.
    /// </summary>
    public static bool ParseDictionary<T>(IDictionary<string, string?>? dictionaryData, out T? result) where T : class, new()
    {
        if (dictionaryData is null)
        {
            result = null;
            return false;
        }

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(dictionaryData);
        var configuration = configurationBuilder.Build();

        result = configuration.Get<T>();
        return result is not null;
    }

    /// <summary>
    ///     Attempts to parse an XML string into an object of type T.
    /// </summary>
    public static bool ParseXml<T>(string? xml, out T? result) where T : class, new()
    {
        if (xml is null)
        {
            result = null;
            return false;
        }

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddXmlStream(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
        var configuration = configurationBuilder.Build();

        result = configuration.Get<T>();
        return result is not null;
    }
}