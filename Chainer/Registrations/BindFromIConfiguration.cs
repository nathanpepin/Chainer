using System.Text.Json;
using Chainer.ChainServices.ChainBuilder.ChainRepository;
using Chainer.ChainServices.ChainBuilder.Messages;
using Chainer.ChainServices.Hashing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chainer.Registrations;

public static class BindFromIConfiguration
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = false };

    /// <summary>
    ///     Binds a section from IConfiguration to a dynamic chain and registers it with the chain repository.
    /// </summary>
    /// <param name="services">The service collection to register services with</param>
    /// <param name="configuration">The configuration source</param>
    /// <param name="name">The name of the chain, used to generate a deterministic GUID</param>
    /// <param name="key">The configuration key to get the section from (defaults to name if empty)</param>
    public static void BindAddChain(this IServiceCollection services, IConfiguration configuration, string name, string key = "")
    {
        var configKey = string.IsNullOrEmpty(key) ? name : key;

        var section = configuration.GetSection(configKey);

        if (!section.Exists()) throw new InvalidOperationException($"Configuration section '{configKey}' not found");

        var chainMessages = DeserializeMessages(section, configKey);

        var chainId = GuidFromString.CreateDeterministicGuid(name);

        FixupAndRegisterChainMessages(services, name, chainMessages, chainId);
        AddChainRepository(services);
    }

    private static List<ChainMessage> DeserializeMessages(IConfigurationSection section, string configKey)
    {
        var messagesSection = section.GetChildren();
        var chainMessages = new List<ChainMessage>();

        foreach (var messageConfig in messagesSection)
        {
            var message = new ChainMessage
            {
                Id = Guid.TryParse(messageConfig["Id"], out var id) ? id : Guid.NewGuid(),
                ExecutionOrder = int.TryParse(messageConfig["ExecutionOrder"], out var order) ? order : 0,
                HandlerTypeName = messageConfig["HandlerTypeName"] ??
                                  throw new InvalidOperationException($"HandlerTypeName missing in chain message configuration under '{configKey}'"),
                ContextTypeName = messageConfig["ContextTypeName"] ??
                                  throw new InvalidOperationException($"ContextTypeName missing in chain message configuration under '{configKey}'")
            };

            // Handle the Configuration section as native JSON
            var configSection = messageConfig.GetSection("Configuration");
            if (configSection.Exists() && configSection.GetChildren().Any())
                // Convert the configuration section to a JSON string
                message.ConfigurationJson = JsonSerializer.Serialize(
                    ConvertConfigurationToObject(configSection), JsonSerializerOptions
                );
            else
                // Fallback to the old ConfigurationJson property if provided
                message.ConfigurationJson = messageConfig["ConfigurationJson"];

            chainMessages.Add(message);
        }

        if (chainMessages.Count == 0) throw new InvalidOperationException($"No chain messages found in configuration section '{configKey}'");

        return chainMessages;
    }

// Helper method to convert IConfigurationSection to a serializable object
    private static object ConvertConfigurationToObject(IConfigurationSection section)
    {
        if (!section.GetChildren().Any())
            // This is a leaf node
            return section.Value;

        // Check if this is an array
        if (section.GetChildren().All(c => int.TryParse(c.Key, out _)))
        {
            var list = new List<object>();
            foreach (var child in section.GetChildren().OrderBy(c => int.Parse(c.Key))) list.Add(ConvertConfigurationToObject(child));

            return list;
        }

        // This is an object
        var dict = new Dictionary<string, object>();
        foreach (var child in section.GetChildren()) dict[child.Key] = ConvertConfigurationToObject(child);

        return dict;
    }

    private static void FixupAndRegisterChainMessages(IServiceCollection services, string name, List<ChainMessage> chainMessages, Guid chainId)
    {
        foreach (var message in chainMessages)
        {
            message.ChainId = chainId;
            message.FriendlyName = name;

            // Generate a new ID for each message if not already set
            if (message.Id == Guid.Empty) message.Id = Guid.NewGuid();

            services.AddSingleton(message);
        }
    }

    private static void AddChainRepository(IServiceCollection services)
    {
        services.TryAddScoped<IChainRepository>(sp =>
        {
            var registeredMessages = sp
                .GetServices<ChainMessage>()
                .OrderBy(x => x.ExecutionOrder)
                .GroupBy(x => x.ChainId);

            var inMemoryChainRepository = new InMemoryChainRepository();

            foreach (var message in registeredMessages) inMemoryChainRepository.SaveChainMessagesAsync(message.Key, message).Wait(); //Ok to wait because its synchronous anyway

            return inMemoryChainRepository;
        });
    }
}