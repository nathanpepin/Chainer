using Chainer.Building.DynamicExecutors;

namespace Chainer.Building.Messages;

/// <summary>
///     Represents a detailed execution log for a single handler in a chain, capturing the 
///     complete lifecycle and state changes during processing.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="ChainExecutionLog"/> class is a central component in Chainer's execution 
///         tracking and monitoring system. It captures detailed information about a handler's execution,
///         including timing, status changes, handler information, and context state before and after processing.
///     </para>
///     <para>
///         This class serves multiple important purposes:
///         <list type="bullet">
///             <item>Auditing the chain execution process for compliance or debugging</item>
///             <item>Monitoring performance by tracking execution times</item>
///             <item>Visualizing the flow of data through a chain</item>
///             <item>Troubleshooting failed executions by identifying which handler failed and why</item>
///             <item>Analyzing the impact of each handler on the context state</item>
///         </list>
///     </para>
///     <para>
///         Execution logs are typically created by the <see cref="DynamicChainExecutor"/> when 
///         processing a chain and are associated with chain repository implementations
///         for persistence. The log's lifecycle follows the handler's execution:
///         <list type="number">
///             <item>Created with status <see cref="ChainMessageStatus.Pending"/></item>
///             <item>Updated to <see cref="ChainMessageStatus.Executing"/> when handler starts</item>
///             <item>Updated to final status (Completed, Failed, Error, or Skipped) when done</item>
///         </list>
///     </para>
///     <para>
///         Context data serialization (BeforeJson and AfterJson) is performed automatically
///         for handlers that implement <see cref="ISaveBeforeContextData"/> and/or
///         <see cref="ISaveAfterContextData"/>.
///     </para>
///     <para>
///         The class is specifically designed to be database-friendly, with properties that map naturally
///         to database columns. It uses simple scalar types (Guid, string, DateTimeOffset, etc.) and
///         serializes complex objects to JSON strings, making it compatible with most relational and
///         document database systems. This design allows for:
///         <list type="bullet">
///             <item>Efficient storage and retrieval of execution history</item>
///             <item>Direct querying of execution status and metadata without deserializing</item>
///             <item>Simple implementation of repository patterns for various database technologies</item>
///             <item>Optimized indexing on key fields like ChainId and Status</item>
///             <item>Flexible storage approaches - from SQL Server tables to document collections</item>
///         </list>
///         Database implementations can use the structured fields for filtering and sorting, while
///         keeping the serialized context state (BeforeJson/AfterJson) as opaque data for storage
///         and retrieval.
///     </para>
/// </remarks>
public class ChainExecutionLog
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChainExecutionLog"/> class
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
    ///     Initializes a new instance of the <see cref="ChainExecutionLog"/> class
    ///     using values from a <see cref="ChainMessage"/>.
    /// </summary>
    /// <param name="message">
    ///     The chain message containing information about the handler to be executed.
    ///     This provides the initial values for the log record.
    /// </param>
    /// <remarks>
    ///     This constructor is typically used by the <see cref="DynamicChainExecutor"/> 
    ///     when preparing to execute a chain. It initializes the log with identifying
    ///     information from the message and sets the initial status to 
    ///     <see cref="ChainMessageStatus.Pending"/>.
    ///     
    ///     Note that <see cref="BeforeJson"/> is initially set to the same value as 
    ///     <see cref="ConfigurationJson"/>, which will typically be overwritten during
    ///     execution if the handler implements <see cref="ISaveBeforeContextData"/>.
    /// </remarks>
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
            ContextTypeName = typeof(TContext).AssemblyQualifiedName!,
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
            ContextTypeName = typeof(TContext).AssemblyQualifiedName!,
        };
    }

    /// <summary>
    ///     Gets or sets the unique identifier for this execution log record.
    /// </summary>
    /// <remarks>
    ///     This ID is typically derived from the associated <see cref="ChainMessage.Id"/>
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
    ///     which allows resolving the type at runtime using <see cref="Type.GetType(string)"/>.
    ///     For display purposes, you may want to extract just the class name portion.
    ///     In database implementations, this field can be used for filtering and reporting
    ///     on specific handler types.
    /// </remarks>
    public string HandlerTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the JSON-serialized configuration that was provided to the handler.
    /// </summary>
    /// <remarks>
    ///     For configurable handlers (those implementing <see cref="IConfigurableChainHandler{TContext}"/>),
    ///     this contains the configuration data that was passed to the handler's Configure method.
    ///     This can be useful for auditing what configuration values were active during execution.
    ///     Storing configuration as a JSON string allows for schema-flexible storage in both relational
    ///     and document databases while preserving the complete configuration state.
    /// </remarks>
    public string? ConfigurationJson { get; set; }

    /// <summary>
    ///     Gets or sets the current status of the handler execution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This status field tracks the handler through its execution lifecycle:
    ///         <list type="bullet">
    ///             <item><see cref="ChainMessageStatus.NotStarted"/> - Initial state before being queued</item>
    ///             <item><see cref="ChainMessageStatus.Pending"/> - Queued for execution but not yet started</item>
    ///             <item><see cref="ChainMessageStatus.Executing"/> - Currently being executed</item>
    ///             <item><see cref="ChainMessageStatus.Completed"/> - Successfully completed execution</item>
    ///             <item><see cref="ChainMessageStatus.Failed"/> - Execution failed (controlled failure)</item>
    ///             <item><see cref="ChainMessageStatus.Error"/> - Execution resulted in an unhandled exception</item>
    ///             <item><see cref="ChainMessageStatus.Skipped"/> - Not executed due to failure of previous handler</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The DynamicChainExecutor updates this status throughout the execution process,
    ///         and it can be used to track the progression of handlers through the chain.
    ///     </para>
    ///     <para>
    ///         From a database perspective, this enumeration field is ideal for indexing and filtering,
    ///         allowing for efficient queries like "find all failed handlers" or "count completed executions."
    ///         It can be stored as an integer in relational databases for performance while maintaining
    ///         semantic meaning through the enum.
    ///     </para>
    /// </remarks>
    public ChainMessageStatus Status { get; set; } = ChainMessageStatus.NotStarted;

    /// <summary>
    ///     Gets or sets the timestamp when the handler started execution.
    /// </summary>
    /// <remarks>
    ///     This is set by the chain executor just before invoking the handler.
    ///     It can be used with <see cref="FinishedAt"/> to calculate execution duration.
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
    ///     When used with <see cref="ExecutedAt"/>, it provides accurate timing information
    ///     for performance analysis and monitoring.
    ///     In database implementations, these timestamp fields enable time-range queries
    ///     and performance analysis for specific periods.
    /// </remarks>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>
    ///     Gets or sets the error message if the handler execution failed.
    /// </summary>
    /// <remarks>
    ///     This field is populated when <see cref="Status"/> is 
    ///     <see cref="ChainMessageStatus.Failed"/> or <see cref="ChainMessageStatus.Error"/>.
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
    ///     and for correctly deserializing the <see cref="BeforeJson"/> and <see cref="AfterJson"/>
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
    ///         <see cref="ISaveBeforeContextData"/> interface. It contains a JSON
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
    ///         <see cref="ISaveAfterContextData"/> interface. It contains a JSON
    ///         representation of the context object after the handler has processed it.
    ///     </para>
    ///     <para>
    ///         When used with <see cref="BeforeJson"/>, it provides a complete before/after
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
}