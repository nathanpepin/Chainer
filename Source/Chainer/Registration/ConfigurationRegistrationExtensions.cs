using Chainer.Abstractions;
using Chainer.Configuration;
using Chainer.Configuration.Binding;
using Chainer.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chainer.Registration;

/// <summary>
///     Provides utilities for registering chain configurations from application configuration
///     settings and binding type names to their fully qualified assembly names.
/// </summary>
public static class ConfigurationRegistrationExtensions
{
    /// <summary>
    ///     Default serialization options for JSON conversion of configuration objects.
    /// </summary>
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = false };

    /// <summary>
    ///     A dictionary used to store mappings between simple keys and the fully qualified assembly names of context and
    ///     handler types.
    /// </summary>
    private static Dictionary<string, string> SimpleTypeMaps { get; } = [];

    /// <summary>
    ///     Adds a mapping between a given key and the assembly-qualified name of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to map to the key</typeparam>
    /// <param name="key">The key associated with the type to be added to the mapping</param>
    public static void AddSimpleTypeMaps<T>(string key)
    {
        var assemblyName = typeof(T).AssemblyQualifiedName!;
        SimpleTypeMaps.Add(key, assemblyName);
    }

    /// <summary>
    ///     Adds a mapping between a key and the assembly-qualified name of a specified type.
    /// </summary>
    /// <param name="key">The key to associate with the type's assembly-qualified name</param>
    /// <param name="type">The type whose assembly-qualified name will be stored</param>
    public static void AddSimpleTypeMaps(string key, Type type)
    {
        var assemblyName = type.AssemblyQualifiedName!;
        SimpleTypeMaps.Add(key, assemblyName);
    }

    /// <summary>
    ///     Adds a mapping between the class name and the assembly-qualified name of a specified type.
    /// </summary>
    /// <typeparam name="T">The type to add to the mapping</typeparam>
    public static void AddSimpleTypeMaps<T>()
    {
        var type = typeof(T);
        var assemblyName = type.AssemblyQualifiedName!;
        SimpleTypeMaps.Add(type.Name, assemblyName);
    }

    /// <summary>
    ///     Registers a chain configuration from an application configuration section and adds it to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the chain configuration to</param>
    /// <param name="configuration">The configuration to load the chain definition from</param>
    /// <param name="sectionName">The name of the configuration section containing the chain definition</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the specified section doesn't exist or required properties are missing
    /// </exception>
    public static void AddChainFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
    {
        var typeMap = SimpleTypeMaps;

        var section = configuration.GetSection(sectionName);
        if (!section.Exists()) throw new InvalidOperationException($"Configuration section '{sectionName}' not found");

        ChainConfigurationGroup chainConfig = new()
        {
            ContextTypeName = section["ContextTypeName"] ??
                              throw new InvalidOperationException($"ContextTypeName missing in chain configuration under '{sectionName}'"),
            Chains = section.GetSection("Chains")
                .GetChildren()
                .Select(x => new ChainConfigurationItem
                {
                    HandlerTypeName = x["HandlerTypeName"] ??
                                      throw new InvalidOperationException($"HandlerTypeName missing in chain configuration under '{sectionName}'"),
                    Configuration = ConvertConfigurationToObject(x.GetSection("Configuration"))
                })
                .ToList()
        };

        chainConfig.ContextTypeName = ReplaceTypeNameIfMapped(chainConfig.ContextTypeName, typeMap);

        foreach (var item in chainConfig.Chains) item.HandlerTypeName = ReplaceTypeNameIfMapped(item.HandlerTypeName, typeMap);

        // Convert to chain messages
        var chainMessages = chainConfig.ToChainMessages(sectionName);

        // Register the messages with the service collection
        foreach (var message in chainMessages) services.AddSingleton(message);

        // Ensure the chain repository is registered
        services.TryAddScoped<IChainRepository>(sp =>
        {
            var registeredMessages = sp
                .GetServices<ChainMessage>()
                .GroupBy(x => x.ChainId);

            var repository = new InMemoryChainRepository();

            foreach (var group in registeredMessages) repository.SaveChainMessagesAsync(group.Key, group.OrderBy(x => x.ExecutionOrder)).Wait();

            return repository;
        });
    }

    /// <summary>
    ///     Resolves a type name against the type map dictionary, returning the mapped name if found.
    /// </summary>
    private static string ReplaceTypeNameIfMapped(string typeName, Dictionary<string, string> typeMap)
    {
        return typeMap.GetValueOrDefault(typeName, typeName);
    }

    /// <summary>
    ///     Converts an IConfigurationSection to a .NET object, handling various section structures.
    /// </summary>
    private static object ConvertConfigurationToObject(IConfigurationSection section)
    {
        if (!section.GetChildren().Any()) return section.Value ?? new object();

        if (section.GetChildren().All(c => int.TryParse(c.Key, out _)))
            return section
                .GetChildren()
                .OrderBy(c => int.Parse(c.Key))
                .Select(ConvertConfigurationToObject)
                .ToList();

        var dict = new Dictionary<string, object>();
        foreach (var child in section.GetChildren()) dict[child.Key] = ConvertConfigurationToObject(child);

        return dict;
    }
}