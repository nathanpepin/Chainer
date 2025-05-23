using Chainer.Execution;

namespace Chainer.Configuration;

/// <summary>
///     Represents a single link in a processing chain, defining a handler to be executed
///     as part of a sequence of operations on a context object.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="ChainMessage" /> class is a fundamental building block in Chainer's dynamic
///         execution system. It defines a single step in a processing chain by specifying:
///         <list type="bullet">
///             <item>Which handler to execute (HandlerTypeName)</item>
///             <item>Its position in the execution sequence (ExecutionOrder)</item>
///             <item>What context type it processes (ContextTypeName)</item>
///             <item>Any configuration data it requires (Configuration)</item>
///         </list>
///     </para>
///     <para>
///         Chain messages form the blueprint for chain execution. They can be:
///         <list type="bullet">
///             <item>Defined in application configuration (JSON, XML)</item>
///             <item>Created programmatically at runtime</item>
///             <item>Stored in databases or other persistence mechanisms</item>
///             <item>Generated through administrative interfaces</item>
///         </list>
///     </para>
///     <para>
///         When executed by the <see cref="DynamicChainExecutor" />, each message is transformed into
///         a handler instance that processes the context in sequence. The executor uses reflection
///         to locate and instantiate the handler type specified in the message.
///     </para>
///     <para>
///         This class is specifically designed to be database-friendly, using simple scalar types
///         and serializing complex configuration to strings. This makes it compatible with most
///         storage systems, from relational databases to document stores, while maintaining the
///         flexibility needed for dynamic chain construction.
///     </para>
///     <para>
///         A collection of ChainMessages with the same ChainId forms a complete processing chain,
///         with their ExecutionOrder determining the sequence of handler execution. This design
///         enables chain definitions to be externalized from code, allowing for runtime reconfiguration
///         of processing pipelines without application redeployment.
///     </para>
/// </remarks>
public sealed class ChainMessage
{
    /// <summary>
    ///     Gets or sets the unique identifier for this chain message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the chain this message belongs to.
    /// </summary>
    public Guid ChainId { get; set; }

    /// <summary>
    ///     Gets or sets a human-readable name for the chain this message belongs to.
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the sequential position of this handler in the chain execution.
    /// </summary>
    public int ExecutionOrder { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified type name of the handler to execute.
    /// </summary>
    public string HandlerTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the format type of the handler configuration data.
    /// </summary>
    public HandlerConfigurationType ConfigurationType { get; set; } = HandlerConfigurationType.Json;

    /// <summary>
    ///     Gets or sets the serialized configuration data for the handler.
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified type name of the context this handler processes.
    /// </summary>
    public string ContextTypeName { get; set; } = string.Empty;
}