using Chainer.Abstractions;
using Chainer.Configuration;

namespace Chainer.Execution;

/// <summary>
///     Represents a detailed execution log for a specific handler in a chain, recording
///     lifecycle events, state transitions, and execution outcomes.
/// </summary>
public class ChainExecutionLog
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChainExecutionLog" /> class
    ///     with default values.
    /// </summary>
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
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the chain this handler execution belongs to.
    /// </summary>
    public Guid ChainId { get; set; }

    /// <summary>
    ///     Gets or sets the sequential position of this handler in the chain.
    /// </summary>
    public int ExecutionOrder { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified type name of the handler that was executed.
    /// </summary>
    public string HandlerTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the JSON-serialized configuration that was provided to the handler.
    /// </summary>
    public string? ConfigurationJson { get; set; }

    /// <summary>
    ///     Gets or sets the current status of the handler execution.
    /// </summary>
    public ChainMessageStatus Status { get; set; } = ChainMessageStatus.NotStarted;

    /// <summary>
    ///     Gets or sets the timestamp when the handler started execution.
    /// </summary>
    public DateTimeOffset? ExecutedAt { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the handler completed execution.
    /// </summary>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>
    ///     Gets or sets the error message if the handler execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified type name of the context processed by this handler.
    /// </summary>
    public string ContextTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the JSON-serialized state of the context before handler execution.
    /// </summary>
    public string? BeforeJson { get; set; }

    /// <summary>
    ///     Gets or sets the JSON-serialized state of the context after handler execution.
    /// </summary>
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