using System.Text.Json;
using Chainer.Building.Messages;
using Chainer.Utilities.Hashing;

namespace Chainer.Building.Configuration;

public sealed class ChainConfigurationGroup
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    public string ContextTypeName { get; internal set; } = string.Empty;
    public List<ChainConfigurationItem> Chains { get; init; } = [];

    public List<ChainMessage> ToChainMessages(string name, HandlerConfigurationType configurationType = HandlerConfigurationType.Json)
    {
        var chainId = GuidFromString.CreateDeterministicGuid(name);

        return Chains
            .Select((x, i) => new ChainMessage
            {
                Id = Guid.NewGuid(),
                ChainId = chainId,
                FriendlyName = name,
                ExecutionOrder = i,
                HandlerTypeName = x.HandlerTypeName,
                ConfigurationType = configurationType,
                Configuration = SerializeConfiguration(x.Configuration),
                ContextTypeName = ContextTypeName
            })
            .ToList();
    }

    public sealed class ChainConfigurationItem
    {
        public string HandlerTypeName { get; internal set; } = string.Empty;
        public object? Configuration { get; internal set; } = string.Empty;
    }

    private static string? SerializeConfiguration(object? configuration)
    {
        return configuration == null
            ? null
            : JsonSerializer.Serialize(configuration, JsonSerializerOptions);
    }
}