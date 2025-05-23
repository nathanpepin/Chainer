namespace Chainer.Configuration.Binding;

/// <summary>
///     Represents a single handler configuration within a chain.
/// </summary>
/// <remarks>
///     Each ChainConfigurationItem defines one step in the processing pipeline,
///     including which handler to use and any configuration data it needs.
/// </remarks>
public sealed class ChainConfigurationItem
{
    /// <summary>
    ///     The fully qualified name of the handler type for this chain step.
    /// </summary>
    /// <remarks>
    ///     You can provide this value in two ways:
    ///     <list type="bullet">
    ///         <item>Use the type's AssemblyQualifiedName (e.g., <c>typeof(MyHandler).AssemblyQualifiedName</c>)</item>
    ///         <item>
    ///             Use a simple name that has been registered via <c>BindFromIConfiguration.AddSimpleTypeMaps&lt;T&gt;()</c>
    ///         </item>
    ///     </list>
    ///     The handler type must implement IChainHandler&lt;TContext&gt; for the context type specified in the parent
    ///     configuration.
    /// </remarks>
    public string HandlerTypeName { get; internal set; } = string.Empty;

    /// <summary>
    ///     Optional configuration data for the handler.
    /// </summary>
    /// <remarks>
    ///     This can be any object that will be serialized to the format specified by ConfigurationType.
    ///     For handlers implementing IConfigurableChainHandler&lt;TContext&gt;, this configuration
    ///     will be passed to their Configure method during chain execution.
    ///     If null, no configuration will be provided to the handler.
    /// </remarks>
    public object? Configuration { get; internal set; } = string.Empty;
}