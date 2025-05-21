using System.Text;
using Microsoft.Extensions.Configuration;

namespace Chainer.Building.Configuration.Extensions;

/// <summary>
/// Provides extension methods for parsing various data formats into strongly-typed configuration objects.
/// </summary>
/// <remarks>
/// This utility class supports the ChainHandlerConfiguration implementation by enabling
/// the conversion of different configuration formats (Object, JSON, Dictionary, XML) into
/// typed objects. It leverages the Microsoft.Extensions.Configuration system for most of the parsing.
/// </remarks>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Attempts to parse an object directly as type T.
    /// </summary>
    /// <typeparam name="T">The target type to convert to</typeparam>
    /// <param name="objectData">The source object to convert</param>
    /// <param name="result">When this method returns, contains the parsed object of type T if successful, or null if parsing failed</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    /// <remarks>
    /// This method performs a direct type cast. It will only succeed if the provided object
    /// is already an instance of type T. No complex conversion or mapping is performed.
    /// </remarks>
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
    /// Attempts to parse a JSON string into an object of type T.
    /// </summary>
    /// <typeparam name="T">The target type to convert to</typeparam>
    /// <param name="jsonText">The JSON string to parse</param>
    /// <param name="result">When this method returns, contains the parsed object of type T if successful, or null if parsing failed</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    /// <remarks>
    /// This method uses Microsoft.Extensions.Configuration to deserialize the JSON string.
    /// It will return false if the JSON string is null, empty, whitespace-only, or an empty object "{}".
    /// Property names in the JSON should match the property names in type T (case-insensitive by default).
    /// </remarks>
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
    /// Attempts to parse a Dictionary&lt;string, string?&gt; into an object of type T.
    /// </summary>
    /// <typeparam name="T">The target type to convert to</typeparam>
    /// <param name="dictionaryData">The dictionary containing configuration key-value pairs</param>
    /// <param name="result">When this method returns, contains the parsed object of type T if successful, or null if parsing failed</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    /// <remarks>
    /// This method uses Microsoft.Extensions.Configuration to map dictionary keys to properties.
    /// It will return false if the dictionary is null.
    /// Dictionary keys should match property names in type T (case-insensitive by default).
    /// Hierarchical configuration is supported using ':' as a separator in keys.
    /// </remarks>
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
    /// Attempts to parse an IDictionary&lt;string, string?&gt; into an object of type T.
    /// </summary>
    /// <typeparam name="T">The target type to convert to</typeparam>
    /// <param name="dictionaryData">The dictionary containing configuration key-value pairs</param>
    /// <param name="result">When this method returns, contains the parsed object of type T if successful, or null if parsing failed</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    /// <remarks>
    /// This overload accepts any IDictionary implementation, not just Dictionary.
    /// It uses the same configuration binding mechanism as the Dictionary overload.
    /// It will return false if the dictionary is null.
    /// </remarks>
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
    /// Attempts to parse an XML string into an object of type T.
    /// </summary>
    /// <typeparam name="T">The target type to convert to</typeparam>
    /// <param name="xml">The XML string to parse</param>
    /// <param name="result">When this method returns, contains the parsed object of type T if successful, or null if parsing failed</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    /// <remarks>
    /// This method uses Microsoft.Extensions.Configuration.Xml to deserialize the XML string.
    /// It will return false if the XML string is null.
    /// XML element names should match property names in type T.
    /// The XML format must be compatible with Microsoft.Extensions.Configuration.Xml parsing rules.
    /// </remarks>
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