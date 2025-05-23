using Chainer.Abstractions;
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
    /// <remarks>
    ///     <para>
    ///         This ID uniquely identifies a specific handler within a chain, enabling:
    ///         <list type="bullet">
    ///             <item>Precise tracking of execution in logs</item>
    ///             <item>Referencing individual chain links in configuration or APIs</item>
    ///             <item>Correlation between messages and their execution logs</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         In database implementations, this serves as the primary key for the message.
    ///         When a message is executed, this ID is typically transferred to the corresponding
    ///         <see cref="ChainExecutionLog" /> to maintain traceability.
    ///     </para>
    /// </remarks>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the chain this message belongs to.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This ID groups related messages into a single chain. All messages with the
    ///         same ChainId form a complete processing pipeline when ordered by their
    ///         <see cref="ExecutionOrder" />.
    ///     </para>
    ///     <para>
    ///         ChainId is used by the <see cref="DynamicChainExecutor" /> and repository
    ///         implementations to retrieve all messages for a chain. It's important that this
    ///         ID remains consistent across all messages in the same chain.
    ///     </para>
    ///     <para>
    ///         For named chains, this ID is typically generated deterministically from the
    ///         <see cref="FriendlyName" /> using <see cref="GuidFromString.CreateDeterministicGuid(string)" />,
    ///         ensuring that the same name always maps to the same ID.
    ///     </para>
    /// </remarks>
    public Guid ChainId { get; set; }

    /// <summary>
    ///     Gets or sets a human-readable name for the chain this message belongs to.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The friendly name provides a more accessible way to reference chains
    ///         in code, configuration, and user interfaces. While <see cref="ChainId" />
    ///         is used internally for lookups, this field makes chains more identifiable
    ///         for humans.
    ///     </para>
    ///     <para>
    ///         This name can be used with
    ///         <see cref="IDynamicChainExecutor.ExecuteChainAsync{TContext}(string,TContext?,System.Threading.CancellationToken)" />
    ///         to execute a chain by name rather than by ID. The executor will convert the name
    ///         to a deterministic GUID for lookup.
    ///     </para>
    ///     <para>
    ///         Examples might include "OrderProcessingChain", "UserRegistrationWorkflow", or
    ///         "DataValidationPipeline", reflecting the business purpose of the chain.
    ///     </para>
    /// </remarks>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the sequential position of this handler in the chain execution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The execution order determines when this handler runs relative to other
    ///         handlers in the same chain. Lower values indicate earlier execution.
    ///     </para>
    ///     <para>
    ///         When the <see cref="DynamicChainExecutor" /> processes a chain, it sorts all
    ///         messages by this field to determine the correct sequence. This allows chain
    ///         definitions to be stored or retrieved in any order while maintaining the
    ///         proper execution sequence.
    ///     </para>
    ///     <para>
    ///         The ordinal sequence should be consistent (no gaps or duplicates) but doesn't
    ///         need to start at any particular value. Common patterns include:
    ///         <list type="bullet">
    ///             <item>Sequential integers (0, 1, 2, 3...)</item>
    ///             <item>Multiples of 10 or 100 (10, 20, 30... or 100, 200, 300...)</item>
    ///         </list>
    ///         Using larger intervals allows for easier insertion of new handlers between
    ///         existing ones without reordering the entire chain.
    ///     </para>
    /// </remarks>
    public int ExecutionOrder { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified type name of the handler to execute.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This should be the assembly-qualified name of a class that implements
    ///         <see cref="IChainHandler{TContext}" /> for the context type specified in
    ///         <see cref="ContextTypeName" />.
    ///     </para>
    ///     <para>
    ///         During execution, the <see cref="DynamicChainExecutor" /> resolves this type
    ///         using <see cref="Type.GetType(string)" /> and then attempts to:
    ///         <list type="number">
    ///             <item>Retrieve an instance from the dependency injection container</item>
    ///             <item>Or create a new instance if not registered in DI</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The type name can be specified in several ways:
    ///         <list type="bullet">
    ///             <item>Using <c>typeof(MyHandler).AssemblyQualifiedName</c> in code</item>
    ///             <item>Using a simple name registered with <c>BindFromIConfiguration.AddSimpleTypeMaps</c></item>
    ///             <item>Directly as a fully qualified name in configuration</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public string HandlerTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the format type of the handler configuration data.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This enum value indicates how the <see cref="Configuration" /> string should be
    ///         interpreted when configuring a handler. The supported types are:
    ///         <list type="bullet">
    ///             <item><see cref="HandlerConfigurationType.Json" /> - JSON-formatted string</item>
    ///             <item><see cref="HandlerConfigurationType.Xml" /> - XML-formatted string</item>
    ///             <item><see cref="HandlerConfigurationType.Dictionary" /> - Serialized dictionary</item>
    ///             <item><see cref="HandlerConfigurationType.Object" /> - Serialized object</item>
    ///             <item><see cref="HandlerConfigurationType.NotSet" /> - No configuration</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The executor uses this value to determine how to parse the <see cref="Configuration" />
    ///         string when creating a <see cref="IHandlerConfiguration" /> object to pass to
    ///         <see cref="IConfigurableChainHandler{TContext}.Configure(IHandlerConfiguration)" />.
    ///     </para>
    ///     <para>
    ///         The default is <see cref="HandlerConfigurationType.Json" />, which is the most common
    ///         format for externalized configuration and works well with most storage systems.
    ///     </para>
    /// </remarks>
    public HandlerConfigurationType ConfigurationType { get; set; } = HandlerConfigurationType.Json;

    /// <summary>
    ///     Gets or sets the serialized configuration data for the handler.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This field contains the configuration data that will be provided to the handler
    ///         if it implements <see cref="IConfigurableChainHandler{TContext}" />. The format of
    ///         this string is determined by <see cref="ConfigurationType" />.
    ///     </para>
    ///     <para>
    ///         For handlers implementing <see cref="IConfigurableChainHandler{TContext}" />, this
    ///         configuration is deserialized and passed to the handler's Configure method before
    ///         execution. Handlers that don't implement this interface will ignore the configuration.
    ///     </para>
    ///     <para>
    ///         Storing configuration as a serialized string enables flexibility in how handlers
    ///         are configured while maintaining compatibility with various storage systems.
    ///         Complex configuration objects can be serialized to JSON or XML for storage and
    ///         then deserialized when needed.
    ///     </para>
    ///     <para>
    ///         Examples of configuration might include:
    ///         <list type="bullet">
    ///             <item>Discount percentages for a pricing handler</item>
    ///             <item>Validation rules for a data validation handler</item>
    ///             <item>Template paths for a notification handler</item>
    ///             <item>API endpoints for an integration handler</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public string? Configuration { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified type name of the context this handler processes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This should be the assembly-qualified name of a class that:
    ///         <list type="bullet">
    ///             <item>Implements <see cref="ICloneable" /></item>
    ///             <item>Has a parameterless constructor</item>
    ///             <item>Is the context type expected by the handler specified in <see cref="HandlerTypeName" /></item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The context type serves several important purposes:
    ///         <list type="bullet">
    ///             <item>Ensuring type compatibility between handlers in a chain</item>
    ///             <item>Enabling creation of new context instances when needed</item>
    ///             <item>Providing type information for serialization/deserialization of context state</item>
    ///             <item>Verification during execution that handlers implement the correct interface</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         All messages in a chain should specify the same context type, as each handler
    ///         in the chain must operate on the same context object type.
    ///     </para>
    /// </remarks>
    public string ContextTypeName { get; set; } = string.Empty;
}