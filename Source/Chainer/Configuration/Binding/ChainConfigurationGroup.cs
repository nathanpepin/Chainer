namespace Chainer.Configuration.Binding;

/// <summary>
///     A data model for storing chain configurations in a structured format, primarily designed
///     for use with application configuration systems like IConfiguration.
/// </summary>
public sealed class ChainConfigurationGroup
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    /// <summary>
    ///     The fully qualified name of the context type that will be processed by this chain.
    /// </summary>
    /// <remarks>
    ///     You can provide this value in two ways:
    ///     <list type="bullet">
    ///         <item>Use the type's AssemblyQualifiedName (e.g., <c>typeof(MyContext).AssemblyQualifiedName</c>)</item>
    ///         <item>
    ///             Use a simple name that has been registered via <c>BindFromIConfiguration.AddSimpleTypeMaps&lt;T&gt;()</c>
    ///         </item>
    ///     </list>
    ///     The context type must implement ICloneable and have a parameterless constructor.
    /// </remarks>
    public string ContextTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     An ordered list of handlers that will process the context in sequence.
    /// </summary>
    /// <remarks>
    ///     The order of handlers in this list determines their execution sequence in the chain.
    ///     Each handler can optionally have its own configuration.
    /// </remarks>
    public List<ChainConfigurationItem> Chains { get; set; } = [];

    /// <summary>
    ///     Converts this configuration group into a list of ChainMessage objects ready for processing.
    /// </summary>
    /// <param name="name">A unique identifier for this chain, used to generate a deterministic GUID</param>
    /// <param name="configurationType">The format of the handler configuration data (defaults to JSON)</param>
    /// <returns>A list of ChainMessage objects that define the complete chain execution flow</returns>
    /// <remarks>
    ///     The order of items in the returned list matches the order in the Chains property.
    ///     Each message includes a unique ID, but shares the same chain ID generated from the name parameter.
    /// </remarks>
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
                Configuration = x.Configuration == null
                    ? null
                    : JsonSerializer.Serialize(x.Configuration, JsonSerializerOptions),
                ContextTypeName = ContextTypeName
            })
            .ToList();
    }
}