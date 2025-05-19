using System.Text;
using Microsoft.Extensions.Configuration;

namespace Chainer.Building.Configuration;

public static class ConfigurationExtensions
{
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