using System.Text.Json;
using Chainer.Building.Configuration;
using Chainer.Building.Configuration.Binding;
using Chainer.Building.Messages;
using Chainer.Building.Repository;
using Chainer.Utilities.Hashing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chainer.Registration;

public static class BindFromIConfiguration
{
    /// <summary>
    /// A dictionary used to store mappings between simple keys and the fully qualified assembly names of context and handler types.
    /// This enables the replacement of handler and context type names in chain messages with their respective fully qualified names.
    /// The mappings are typically added using the AddSimpleTypeMaps methods.
    /// </summary>
    internal static Dictionary<string, string> SimpleTypeMaps { get; } = [];

    /// <summary>
    /// Adds a mapping between a given key and the assembly-qualified name of the specified type.
    /// </summary>
    /// <param name="key">The key associated with the type to be added to the mapping</param>
    public static void AddSimpleTypeMaps<T>(string key)
    {
        var assemblyName = typeof(T).AssemblyQualifiedName!;
        SimpleTypeMaps.Add(key, assemblyName);
    }

    /// <summary>
    /// Adds a mapping between a key and the assembly-qualified name of a specified type to the simple type maps collection.
    /// </summary>
    /// <param name="key">The key to associate with the type's assembly-qualified name</param>
    /// <param name="type">The type whose assembly-qualified name will be stored</param>
    public static void AddSimpleTypeMaps(string key, Type type)
    {
        var assemblyName = type.AssemblyQualifiedName!;
        SimpleTypeMaps.Add(key, assemblyName);
    }

    /// <summary>
    /// Adds a mapping between the class name and the assembly-qualified name of a specified type to the simple type maps collection.
    /// </summary>
    public static void AddSimpleTypeMaps<T>()
    {
        var type = typeof(T);
        var assemblyName = type.AssemblyQualifiedName!;
        SimpleTypeMaps.Add(type.Name, assemblyName);
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = false };

    public static void AddChainFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
    {
        var typeMap = BindFromIConfiguration.SimpleTypeMaps;

        var section = configuration.GetSection(sectionName);
        if (!section.Exists())
        {
            throw new InvalidOperationException($"Configuration section '{sectionName}' not found");
        }

        ChainConfigurationGroup chainConfig = new()
        {
            ContextTypeName = section["ContextTypeName"] ??
                              throw new InvalidOperationException($"ContextTypeName missing in chain configuration under '{sectionName}'"),
            Chains = section.GetSection("Chains")
                .GetChildren()
                .Select(x => new ChainConfigurationGroup.ChainConfigurationItem
                {
                    HandlerTypeName = x["HandlerTypeName"] ??
                                      throw new InvalidOperationException($"HandlerTypeName missing in chain configuration under '{sectionName}'"),
                    Configuration = ConvertConfigurationToObject(x.GetSection("Configuration"))
                })
                .ToList()
        };

        chainConfig.ContextTypeName = ReplaceTypeNameIfMapped(chainConfig.ContextTypeName, typeMap);

        foreach (var item in chainConfig.Chains)
        {
            item.HandlerTypeName = ReplaceTypeNameIfMapped(item.HandlerTypeName, typeMap);
        }

        // Convert to chain messages
        var chainMessages = chainConfig.ToChainMessages(sectionName);

        // Register the messages with the service collection
        foreach (var message in chainMessages)
        {
            services.AddSingleton(message);
        }

        // Ensure the chain repository is registered
        services.TryAddScoped<IChainRepository>(sp =>
        {
            var registeredMessages = sp
                .GetServices<ChainMessage>()
                .GroupBy(x => x.ChainId);

            var repository = new InMemoryChainRepository();

            foreach (var group in registeredMessages)
            {
                repository.SaveChainMessagesAsync(group.Key, group.OrderBy(x => x.ExecutionOrder)).Wait();
            }

            return repository;
        });
    }

    private static string ReplaceTypeNameIfMapped(string typeName, Dictionary<string, string> typeMap)
    {
        return typeMap.GetValueOrDefault(typeName, typeName);
    }

    private static object ConvertConfigurationToObject(IConfigurationSection section)
    {
        if (!section.GetChildren().Any())
        {
            return section.Value ?? new object();
        }

        if (section.GetChildren().All(c => int.TryParse(c.Key, out _)))
        {
            return section
                .GetChildren()
                .OrderBy(c => int.Parse(c.Key))
                .Select(ConvertConfigurationToObject)
                .ToList();
        }

        var dict = new Dictionary<string, object>();
        foreach (var child in section.GetChildren())
        {
            dict[child.Key] = ConvertConfigurationToObject(child);
        }

        return dict;
    }
}