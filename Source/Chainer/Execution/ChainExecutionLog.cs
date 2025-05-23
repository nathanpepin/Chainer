using Chainer.Abstractions;
using Chainer.Configuration;

namespace Chainer.Execution;

/// <summary>
///     Represents a detailed execution log for a specific handler in a chain, recording
///     lifecycle events, state transitions, and execution outcomes.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="ChainExecutionLog" /> encapsulates information about the operation
///         and status of individual handlers during the processing of a chain in the Chainer framework.
///         This includes metadata such as execution timestamps, configuration details, error messages,
///         and the serialized state of the execution context.
///     </para>
///     <para>
///         Key benefits and purposes include:
///         <list type="bullet">
///             <item>Facilitating process visibility by capturing step-level execution details</item>
///             <item>Providing insights for debugging and diagnosing failures</item>
///             <item>Supporting performance analysis by logging detailed timing information</item>
///             <item>Tracking impact of changes on execution state for auditing and compliance</item>
///             <item>Enabling persistence or downstream processing of detailed execution logs</item>
///         </list>
///     </para>
///     <para>
///         Each <see cref="ChainExecutionLog" /> instance is associated with a specific handler
///         invocation within a processing chain, and its stages of execution typically include:
///         <list type="number">
///             <item>Initialization prior to handler execution</item>
///             <item>Status updates when execution starts or progresses</item>
///             <item>Finalization upon success, failure, or error</item>
///         </list>
///     </para>
///     <para>
///         Serialization of context properties (via <see cref="BeforeJson" /> and <see cref="AfterJson" />)
///         is automatically performed for handlers utilizing supported configuration interfaces, providing
///         a full snapshot of the execution state before and after processing.
///     </para>
///     <para>
///         The class is designed for extensibility and database compatibility, ensuring its properties
///         naturally align with storage and analytics operations.
///     </para>
/// </remarks>
public class ChainExecutionLog
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChainExecutionLog" /> class
    ///     with default values.
    /// </summary>
    /// <remarks>
    ///     When using this constructor, all properties must be set manually before
    ///     the log record is used.
    /// </remarks>
    public ChainExecutionLog()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChainExecutionLog" /> class
    ///     using values from a <see cref="ChainMessage" />.
    /// </summary>
    public ChainExecutionLog(ChainMessage message)
    {
        Id = message.Id;
        ChainId = message.ChainId;
        ExecutionOrder = message.ExecutionOrder;
        HandlerTypeName = message.HandlerTypeName;
        ConfigurationJson = message.Configuration;
        ContextTypeName = message.ContextTypeName;
        BeforeJson = message.Configuration;
        Status = ChainMessageStatus.Pending;
    }

    /// <summary>
    ///     Gets or sets the unique identifier for this execution log record.
    /// </summary>
    /// <remarks>
    ///     This ID is typically derived from the associated <see cref="ChainMessage.Id" />
    ///     to maintain a correlation between messages and their execution logs.
    ///     It serves as the primary key in database implementations.
    /// </remarks>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the chain this handler execution belongs to.
    /// </summary>
    /// <remarks>
    ///     This enables grouping all execution logs for a single chain execution.
    ///     Multiple handlers executed as part of the same chain will share this ID.
    ///     In database terms, this field is ideal for indexing and serves as a foreign key
    ///     to the chain definition table, enabling efficient queries for all logs related
    ///     to a specific chain execution.
    /// </remarks>
    public Guid ChainId { get; set; }

    /// <summary>
    ///     Gets or sets the sequential position of this handler in the chain.
    /// </summary>
    /// <remarks>
    ///     The execution order determines the sequence in which handlers are processed.
    ///     Lower values indicate earlier execution in the chain.
    ///     This value is used to sort logs when displaying or analyzing the chain flow.
    ///     When stored in a database, this field allows for ordered retrieval of logs
    ///     without requiring additional sorting operations.
    /// </remarks>
    public int ExecutionOrder { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified type name of the handler that was executed.
    /// </summary>
    /// <remarks>
    ///     This is typically the assembly-qualified name of the handler class,
    ///     which allows resolving the type at runtime using <see cref="Type.GetType(string)" />.
    ///     For display purposes, you may want to extract just the class name portion.
    ///     In database implementations, this field can be used for filtering and reporting
    ///     on specific handler types.
    /// </remarks>
    public string HandlerTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the JSON-serialized configuration that was provided to the handler.
    /// </summary>
    /// <remarks>
    ///     For configurable handlers (those implementing <see cref="IConfigurableChainHandler{TContext}" />),
    ///     this contains the configuration data that was passed to the handler's Configure method.
    ///     This can be useful for auditing what configuration values were active during execution.
    ///     Storing configuration as a JSON string allows for schema-flexible storage in both relational
    ///     and document databases while preserving the complete configuration state.
    /// </remarks>
    public string? ConfigurationJson { get; set; }

    /// <summary>
    ///     Gets or sets the current status of the handler execution.
    /// </summary>
    public ChainMessageStatus Status { get; set; } = ChainMessageStatus.NotStarted;

    /// <summary>
    ///     Gets or sets the timestamp when the handler started execution.
    /// </summary>
    /// <remarks>
    ///     This is set by the chain executor just before invoking the handler.
    ///     It can be used with <see cref="FinishedAt" /> to calculate execution duration.
    ///     Using DateTimeOffset provides timezone-aware timestamps that avoid ambiguity
    ///     when stored in databases and analyzed across different systems.
    /// </remarks>
    public DateTimeOffset? ExecutedAt { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the handler completed execution.
    /// </summary>
    /// <remarks>
    ///     This is set by the chain executor after the handler completes,
    ///     regardless of whether it succeeded or failed.
    ///     When used with <see cref="ExecutedAt" />, it provides accurate timing information
    ///     for performance analysis and monitoring.
    ///     In database implementations, these timestamp fields enable time-range queries
    ///     and performance analysis for specific periods.
    /// </remarks>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>
    ///     Gets or sets the error message if the handler execution failed.
    /// </summary>
    /// <remarks>
    ///     This field is populated when <see cref="Status" /> is
    ///     <see cref="ChainMessageStatus.Failed" /> or <see cref="ChainMessageStatus.Error" />.
    ///     It contains the error message from the Result or Exception that caused the failure,
    ///     providing valuable diagnostic information.
    ///     When stored in a database, this field enables text searching for specific error patterns
    ///     across multiple executions.
    /// </remarks>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified type name of the context processed by this handler.
    /// </summary>
    /// <remarks>
    ///     This is the assembly-qualified name of the context class,
    ///     which is useful for determining what type of data was being processed
    ///     and for correctly deserializing the <see cref="BeforeJson" /> and <see cref="AfterJson" />
    ///     values if needed.
    ///     In a database context, this field allows for filtering and reporting on executions
    ///     by context type.
    /// </remarks>
    public string ContextTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the JSON-serialized state of the context before handler execution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This field is only populated when the handler implements the
    ///         <see cref="IPostExecutionPersistence" /> interface. It contains a JSON
    ///         representation of the context object just before the handler processes it.
    ///     </para>
    ///     <para>
    ///         This is valuable for:
    ///         <list type="bullet">
    ///             <item>Debugging by examining the input state to a handler</item>
    ///             <item>Auditing to track what data was available at each step</item>
    ///             <item>Analysis to determine how context changes throughout the chain</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Storing context state as JSON strings provides a database-friendly approach that works
    ///         with both relational (using text/JSON columns) and document databases. It avoids
    ///         the need for complex object-relational mapping while preserving the complete state
    ///         for later analysis or recreation of the execution environment.
    ///     </para>
    /// </remarks>
    public string? BeforeJson { get; set; }

    /// <summary>
    ///     Gets or sets the JSON-serialized state of the context after handler execution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This field is only populated when the handler implements the
    ///         <see cref="IPreExecutionPersistence" /> interface. It contains a JSON
    ///         representation of the context object after the handler has processed it.
    ///     </para>
    ///     <para>
    ///         When used with <see cref="BeforeJson" />, it provides a complete before/after
    ///         view of the context, showing exactly what changes the handler made.
    ///         This is particularly useful for:
    ///         <list type="bullet">
    ///             <item>Verifying that handlers are working as expected</item>
    ///             <item>Debugging issues where context is improperly modified</item>
    ///             <item>Creating a detailed audit trail of all data changes</item>
    ///             <item>Recreating the execution flow for analysis or reporting</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Like BeforeJson, this field's JSON string format enables efficient storage in various
    ///         database types while maintaining the ability to analyze or recreate the exact state.
    ///         Modern databases with JSON capabilities (SQL Server, PostgreSQL) can even query into
    ///         these JSON fields for specialized reporting needs.
    ///     </para>
    /// </remarks>
    public string? AfterJson { get; set; }


    /// <summary>
    ///     Creates a new instance of the <see cref="ChainExecutionLog" /> class
    ///     initialized with the specified chain handler and execution order.
    /// </summary>
    public static ChainExecutionLog Create<TContext>(IChainHandler<TContext> chainHandler, int order)
        where TContext : class, ICloneable, new()
    {
        return new ChainExecutionLog
        {
            Id = Guid.Empty,
            ChainId = Guid.Empty,
            ExecutionOrder = order,
            HandlerTypeName = chainHandler.GetType().FullName!,
            Status = ChainMessageStatus.NotStarted,
            ContextTypeName = typeof(TContext).AssemblyQualifiedName!
        };
    }

    public static ChainExecutionLog Create<TContext>(Type type, int order)
        where TContext : class, ICloneable, new()
    {
        return new ChainExecutionLog
        {
            Id = Guid.Empty,
            ChainId = Guid.Empty,
            ExecutionOrder = order,
            HandlerTypeName = type.FullName!,
            Status = ChainMessageStatus.NotStarted,
            ContextTypeName = typeof(TContext).AssemblyQualifiedName!
        };
    }
}