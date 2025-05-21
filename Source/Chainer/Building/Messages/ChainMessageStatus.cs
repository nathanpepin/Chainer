

namespace Chainer.Building.Messages;

/// <summary>
///     Defines the possible execution states of a handler within a chain, tracking its 
///     progression from initialization through completion or failure.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="ChainMessageStatus"/> enum is a critical component in Chainer's execution 
///         tracking system. It provides a standardized set of states that represent the lifecycle
///         of a handler as it progresses through the execution pipeline.
///     </para>
///     <para>
///         This enum is primarily used in the <see cref="ChainExecutionLog"/> class to track 
///         and report on the execution status of individual handlers. The <see cref="DynamicChainExecutor"/>
///         updates this status at key points during the execution process, providing a detailed
///         view of how the chain is progressing.
///     </para>
///     <para>
///         Status transitions typically follow this sequence:
///         <list type="number">
///             <item><see cref="NotStarted"/> → <see cref="Pending"/> (when loaded by executor)</item>
///             <item><see cref="Pending"/> → <see cref="Executing"/> (when handler begins execution)</item>
///             <item><see cref="Executing"/> → <see cref="Completed"/>/<see cref="Failed"/>/<see cref="Error"/> (after execution)</item>
///         </list>
///         Alternatively, a handler might be marked as <see cref="Skipped"/> if a previous handler 
///         in the chain failed, preventing its execution.
///     </para>
///     <para>
///         This enum is designed to be database-friendly, using distinct integer values that can be
///         efficiently stored and indexed. The ordered values also reflect the general progression
///         of states, with terminal states (Completed, Skipped, Failed, Error) having higher values.
///     </para>
/// </remarks>
public enum ChainMessageStatus
{
    /// <summary>
    ///     The initial state for a handler that has been defined but not yet queued for execution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is the default state for newly created <see cref="ChainExecutionLog"/> instances
    ///         before they are associated with a specific execution request.
    ///     </para>
    ///     <para>
    ///         Handlers in this state have been defined in the chain but haven't yet been 
    ///         processed by the executor. They are effectively in a pre-execution state.
    ///     </para>
    ///     <para>
    ///         Logs in this state typically don't have execution timestamps or other 
    ///         execution-related fields populated.
    ///     </para>
    /// </remarks>
    NotStarted = 0,

    /// <summary>
    ///     Indicates that the handler has been queued for execution but has not yet started processing.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This state signals that the handler has been loaded by the executor and is awaiting
    ///         its turn in the execution sequence. It is the normal state after a 
    ///         <see cref="ChainExecutionLog"/> is created from a <see cref="ChainMessage"/> and
    ///         before execution begins.
    ///     </para>
    ///     <para>
    ///         The transition from <see cref="NotStarted"/> to <see cref="Pending"/> typically
    ///         occurs when the executor prepares the handler for execution, such as when initializing
    ///         execution logs at the start of a chain execution.
    ///     </para>
    ///     <para>
    ///         Handlers remain in this state until they become the active handler in the chain.
    ///     </para>
    /// </remarks>
    Pending,

    /// <summary>
    ///     Indicates that the handler is currently being executed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This state represents an active execution in progress. When a handler enters this state:
    ///         <list type="bullet">
    ///             <item>The <see cref="ChainExecutionLog.ExecutedAt"/> timestamp is set</item>
    ///             <item>The executor has resolved the handler instance</item>
    ///             <item>Any configuration has been applied (for configurable handlers)</item>
    ///             <item>The handler's Handle method is about to be invoked or is executing</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This is generally a transient state that exists only during the actual execution
    ///         of the handler. In monitoring systems, handlers that remain in this state for 
    ///         an extended period might indicate a stalled or hung execution.
    ///     </para>
    ///     <para>
    ///         This state is updated to a terminal state (<see cref="Completed"/>, <see cref="Failed"/>,
    ///         or <see cref="Error"/>) once the handler's execution completes.
    ///     </para>
    /// </remarks>
    Executing,

    /// <summary>
    ///     Indicates that the handler executed successfully without errors.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a terminal state representing successful execution of the handler. 
    ///         It indicates that:
    ///         <list type="bullet">
    ///             <item>The handler's Handle method completed execution</item>
    ///             <item>The method returned a successful Result</item>
    ///             <item>No exceptions were thrown during execution</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         When a handler reaches this state, its <see cref="ChainExecutionLog.FinishedAt"/> 
    ///         timestamp is set, and its <see cref="ChainExecutionLog.AfterJson"/> may be populated
    ///         if the handler implements <see cref="ISaveAfterContextData"/>.
    ///     </para>
    ///     <para>
    ///         In most cases, the chain execution will continue to the next handler after
    ///         one reaches this state. This state indicates that the handler's contribution
    ///         to the chain has been successfully completed.
    ///     </para>
    /// </remarks>
    Completed,

    /// <summary>
    ///     Indicates that the handler was not executed because a previous handler in the chain failed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a terminal state representing a handler that was not executed due to the
    ///         failure of a preceding handler in the chain. Since chain execution stops at the first
    ///         failure, all subsequent handlers are marked as skipped.
    ///     </para>
    ///     <para>
    ///         Handlers in this state:
    ///         <list type="bullet">
    ///             <item>Were never instantiated or executed</item>
    ///             <item>Did not receive any context data</item>
    ///             <item>Did not contribute to the chain result</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The presence of skipped handlers in an execution log typically indicates a chain
    ///         execution that ended prematurely due to a failure. Analyzing which handlers were
    ///         skipped can help understand what processing steps were missed.
    ///     </para>
    /// </remarks>
    Skipped,

    /// <summary>
    ///     Indicates that the handler executed but returned a failure result.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a terminal state representing controlled failure of a handler. It differs
    ///         from <see cref="Error"/> in that it represents an expected failure path rather than
    ///         an exceptional condition. It indicates that:
    ///         <list type="bullet">
    ///             <item>The handler's Handle method completed execution</item>
    ///             <item>The method explicitly returned a failure Result</item>
    ///             <item>No unhandled exceptions occurred during execution</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         When a handler reaches this state:
    ///         <list type="bullet">
    ///             <item>Chain execution stops at this handler</item>
    ///             <item>Subsequent handlers are marked as <see cref="Skipped"/></item>
    ///             <item>The <see cref="ChainExecutionLog.ErrorMessage"/> field contains the failure reason</item>
    ///             <item>The <see cref="ChainExecutionLog.FinishedAt"/> timestamp is set</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This state often represents a business rule violation or validation failure rather
    ///         than a technical error. For example, a validation handler might fail if the input
    ///         data doesn't meet requirements, returning a meaningful error message that explains
    ///         the validation problem.
    ///     </para>
    /// </remarks>
    Failed,

    /// <summary>
    ///     Indicates that the handler threw an exception or encountered an unexpected error during execution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a terminal state representing an exceptional condition or unhandled error
    ///         during handler execution. It indicates that:
    ///         <list type="bullet">
    ///             <item>An unhandled exception was thrown during handler initialization or execution</item>
    ///             <item>Or a system error prevented normal execution</item>
    ///             <item>The exception was caught by the executor's error handling</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         When a handler reaches this state:
    ///         <list type="bullet">
    ///             <item>Chain execution stops at this handler</item>
    ///             <item>Subsequent handlers are marked as <see cref="Skipped"/></item>
    ///             <item>The <see cref="ChainExecutionLog.ErrorMessage"/> field contains the exception message</item>
    ///             <item>The <see cref="ChainExecutionLog.FinishedAt"/> timestamp is set</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This state differs from <see cref="Failed"/> in that it represents an unexpected 
    ///         technical error rather than a controlled failure path. While <see cref="Failed"/> 
    ///         might indicate invalid input or a business rule violation, <see cref="Error"/> 
    ///         typically indicates a bug, system failure, or unhandled edge case.
    ///     </para>
    ///     <para>
    ///         Handlers in this state should be investigated as they may indicate problems in the 
    ///         code, configuration, or runtime environment.
    ///     </para>
    /// </remarks>
    Error
}