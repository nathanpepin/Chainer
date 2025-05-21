using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chainer.Registration;

/// <summary>
///     Provides utilities for registering chain configurations from application configuration
///     settings and binding type names to their fully qualified assembly names.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="BindFromIConfiguration"/> class is a core component of Chainer's
///         configuration-driven chain system. It enables the definition of chains in application
///         configuration files (such as appsettings.json) and handles the conversion of these
///         definitions to runtime chain messages.
///     </para>
///     <para>
///         This class solves a key challenge in configuration-based chains: the need to reference
///         types by name in configuration while ensuring those names can be resolved to actual
///         types at runtime. It accomplishes this through a type mapping system that allows
///         simple names in configuration to be mapped to fully qualified assembly names.
///     </para>
///     <para>
///         The typical usage flow is:
///         <list type="number">
///             <item>Register type mappings using one of the AddSimpleTypeMaps methods</item>
///             <item>Call AddChainFromConfiguration to register chains from configuration</item>
///             <item>Use the registered services with DynamicChainExecutor</item>
///         </list>
///     </para>
///     <para>
///         For example, in your application startup:
///         <code>
///         // Register type mappings
///         BindFromIConfiguration.AddSimpleTypeMaps&lt;OrderContext&gt;();
///         BindFromIConfiguration.AddSimpleTypeMaps&lt;ValidateOrderHandler&gt;();
///         BindFromIConfiguration.AddSimpleTypeMaps&lt;ProcessPaymentHandler&gt;();
///         
///         // Register chains from configuration
///         services.AddChainFromConfiguration(configuration, "OrderProcessingChain");
///         </code>
///     </para>
///     <para>
///         This approach provides several benefits:
///         <list type="bullet">
///             <item>Separation of chain definition from code</item>
///             <item>Runtime reconfiguration without application redeployment</item>
///             <item>Environment-specific chain configurations</item>
///             <item>Simplified type references in configuration files</item>
///         </list>
///     </para>
/// </remarks>
public static class BindFromIConfiguration
{
    /// <summary>
    ///     Default serialization options for JSON conversion of configuration objects.
    /// </summary>
    /// <remarks>
    ///     These options are used when serializing configuration objects to JSON format.
    ///     The WriteIndented option is set to false to produce compact JSON strings
    ///     suitable for storage in configuration.
    /// </remarks>
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = false };

    /// <summary>
    ///     A dictionary used to store mappings between simple keys and the fully qualified assembly names of context and
    ///     handler types.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This dictionary is the heart of the type mapping system. It serves as a lookup table
    ///         that maps simple type names (as used in configuration files) to fully qualified 
    ///         assembly names (as required for runtime type resolution).
    ///     </para>
    ///     <para>
    ///         For example, it might contain a mapping from "OrderContext" to 
    ///         "MyCompany.OrderProcessing.Models.OrderContext, MyCompany.OrderProcessing, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null".
    ///     </para>
    ///     <para>
    ///         This dictionary is populated by the various AddSimpleTypeMaps methods and is used
    ///         by the AddChainFromConfiguration method to resolve type names from configuration.
    ///     </para>
    ///     <para>
    ///         The dictionary is static and shared across all uses of BindFromIConfiguration,
    ///         allowing for centralized type registration at application startup.
    ///     </para>
    /// </remarks>
    internal static Dictionary<string, string> SimpleTypeMaps { get; } = [];

    /// <summary>
    ///     Adds a mapping between a given key and the assembly-qualified name of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to map to the key</typeparam>
    /// <param name="key">The key associated with the type to be added to the mapping</param>
    /// <remarks>
    ///     <para>
    ///         This method creates a mapping from a custom key to the assembly-qualified name
    ///         of the specified type. This is useful when the keys in configuration don't match
    ///         the actual type names.
    ///     </para>
    ///     <para>
    ///         For example:
    ///         <code>
    ///         // Maps "Order" to the assembly-qualified name of OrderContext
    ///         BindFromIConfiguration.AddSimpleTypeMaps&lt;OrderContext&gt;("Order");
    ///         </code>
    ///     </para>
    ///     <para>
    ///         After this mapping is added, a configuration entry like:
    ///         <code>
    ///         "ContextTypeName": "Order"
    ///         </code>
    ///         will be resolved to the fully qualified OrderContext type.
    ///     </para>
    ///     <para>
    ///         If a mapping for the key already exists, it will be overwritten with the new value.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     <para>
    ///         This method is similar to <see cref="AddSimpleTypeMaps{T}(string)"/> but accepts
    ///         a Type object directly instead of using a generic type parameter. This is useful
    ///         when the type is not known at compile time or when working with reflection.
    ///     </para>
    ///     <para>
    ///         For example:
    ///         <code>
    ///         // Get a type through reflection
    ///         var handlerType = Assembly.GetExecutingAssembly().GetType("MyNamespace.MyHandler");
    ///         
    ///         // Map "CustomHandler" to this type
    ///         BindFromIConfiguration.AddSimpleTypeMaps("CustomHandler", handlerType);
    ///         </code>
    ///     </para>
    ///     <para>
    ///         After this mapping is added, a configuration entry like:
    ///         <code>
    ///         "HandlerTypeName": "CustomHandler"
    ///         </code>
    ///         will be resolved to the fully qualified handler type.
    ///     </para>
    ///     <para>
    ///         If a mapping for the key already exists, it will be overwritten with the new value.
    ///     </para>
    /// </remarks>
    public static void AddSimpleTypeMaps(string key, Type type)
    {
        var assemblyName = type.AssemblyQualifiedName!;
        SimpleTypeMaps.Add(key, assemblyName);
    }

    /// <summary>
    ///     Adds a mapping between the class name and the assembly-qualified name of a specified type.
    /// </summary>
    /// <typeparam name="T">The type to add to the mapping</typeparam>
    /// <remarks>
    ///     <para>
    ///         This method creates a mapping from the simple class name to the assembly-qualified
    ///         name of the specified type. This is the most common mapping approach, allowing
    ///         configuration to use simple class names without namespace or assembly information.
    ///     </para>
    ///     <para>
    ///         For example:
    ///         <code>
    ///         // Maps "OrderContext" to the assembly-qualified name of OrderContext
    ///         BindFromIConfiguration.AddSimpleTypeMaps&lt;OrderContext&gt;();
    ///         </code>
    ///     </para>
    ///     <para>
    ///         After this mapping is added, a configuration entry like:
    ///         <code>
    ///         "ContextTypeName": "OrderContext"
    ///         </code>
    ///         will be resolved to the fully qualified OrderContext type.
    ///     </para>
    ///     <para>
    ///         This method is particularly useful for common scenarios where the configuration
    ///         simply uses the class name without any custom key. It reduces the amount of
    ///         mapping code required for typical applications.
    ///     </para>
    ///     <para>
    ///         If a mapping for the class name already exists, it will be overwritten with the new value.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     <para>
    ///         This method is the core of the configuration-based chain system. It:
    ///         <list type="number">
    ///             <item>Loads a chain definition from a configuration section</item>
    ///             <item>Resolves type names using the <see cref="SimpleTypeMaps"/> dictionary</item>
    ///             <item>Converts the configuration to <see cref="ChainMessage"/> objects</item>
    ///             <item>Registers these messages with the service collection</item>
    ///             <item>Ensures an <see cref="IChainRepository"/> is registered</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The configuration section must have the following structure:
    ///         <code>
    ///         {
    ///           "SectionName": {
    ///             "ContextTypeName": "MyContext",
    ///             "Chains": [
    ///               {
    ///                 "HandlerTypeName": "FirstHandler",
    ///                 "Configuration": {
    ///                   "Property1": "Value1",
    ///                   "Property2": 42
    ///                 }
    ///               },
    ///               {
    ///                 "HandlerTypeName": "SecondHandler"
    ///               }
    ///             ]
    ///           }
    ///         }
    ///         </code>
    ///     </para>
    ///     <para>
    ///         The ContextTypeName and HandlerTypeName values are resolved against the
    ///         <see cref="SimpleTypeMaps"/> dictionary to convert simple names to fully
    ///         qualified assembly names.
    ///     </para>
    ///     <para>
    ///         This method also registers an <see cref="InMemoryChainRepository"/> with the
    ///         service collection if no implementation of <see cref="IChainRepository"/> is
    ///         already registered. This repository will be populated with the chain messages
    ///         created from the configuration.
    ///     </para>
    ///     <para>
    ///         To use a different repository implementation, register it before calling this method:
    ///         <code>
    ///         // Register a custom repository
    ///         services.AddScoped&lt;IChainRepository, MyCustomRepository&gt;();
    ///         
    ///         // Register chains from configuration
    ///         services.AddChainFromConfiguration(configuration, "MyChain");
    ///         </code>
    ///     </para>
    /// </remarks>
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
                .Select(x => new ChainConfigurationGroup.ChainConfigurationItem
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
    /// <param name="typeName">The type name to resolve</param>
    /// <param name="typeMap">The dictionary of type name mappings</param>
    /// <returns>
    ///     The mapped type name if found in the dictionary, otherwise the original type name
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This helper method attempts to look up a type name in the provided mapping dictionary.
    ///         If the name is found, the corresponding fully qualified assembly name is returned.
    ///         If not found, the original name is returned unchanged.
    ///     </para>
    ///     <para>
    ///         This approach allows for seamless handling of both simple names (which are mapped)
    ///         and already-qualified names (which pass through unchanged). It also gracefully
    ///         handles cases where a mapping hasn't been registered for a particular type name.
    ///     </para>
    ///     <para>
    ///         For example:
    ///         <list type="bullet">
    ///             <item>"OrderContext" might resolve to "MyCompany.OrderProcessing.Models.OrderContext, MyCompany.OrderProcessing, ..."</item>
    ///             <item>"MyCompany.Other.Context, ..." would remain unchanged if no mapping exists</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    private static string ReplaceTypeNameIfMapped(string typeName, Dictionary<string, string> typeMap)
    {
        return typeMap.GetValueOrDefault(typeName, typeName);
    }

    /// <summary>
    ///     Converts an IConfigurationSection to a .NET object, handling various section structures.
    /// </summary>
    /// <param name="section">The configuration section to convert</param>
    /// <returns>
    ///     A .NET object representing the configuration section:
    ///     - A string value for leaf sections
    ///     - A List for array sections
    ///     - A Dictionary for object sections
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This helper method recursively converts an IConfigurationSection hierarchy into
    ///         a corresponding hierarchy of .NET objects. It handles three main section types:
    ///         <list type="bullet">
    ///             <item>Leaf sections (with no children) are converted to string values</item>
    ///             <item>Array sections (with numeric keys) are converted to Lists</item>
    ///             <item>Object sections (with named keys) are converted to Dictionaries</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This conversion enables the configuration data to be serialized to JSON and
    ///         stored in <see cref="ChainMessage"/> objects for use by handlers.
    ///     </para>
    ///     <para>
    ///         The conversion maintains the hierarchical structure of the configuration,
    ///         allowing for complex nested configurations to be properly represented.
    ///     </para>
    ///     <para>
    ///         For example, a configuration section like:
    ///         <code>
    ///         {
    ///           "Property1": "Value1",
    ///           "Nested": {
    ///             "SubProperty": 42
    ///           },
    ///           "Array": [
    ///             "Item1",
    ///             "Item2"
    ///           ]
    ///         }
    ///         </code>
    ///         would be converted to a dictionary with a string value, a nested dictionary,
    ///         and a list.
    ///     </para>
    /// </remarks>
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