namespace Chainer.Building.Messages;

/// <summary>
///     Defines the possible execution states of a handler within a chain, tracking its
///     progression from initialization through completion or failure.
/// </summary>
public enum ChainMessageStatus
{
    /// <summary>
    ///     The initial state for a handler that has been defined but not yet queued for execution.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    ///     Indicates that the handler has been queued for execution but has not yet started processing.
    /// </summary>
    Pending,

    /// <summary>
    ///     Indicates that the handler is currently being executed.
    /// </summary>
    Executing,

    /// <summary>
    ///     Indicates that the handler executed successfully without errors.
    /// </summary>
    Completed,

    /// <summary>
    ///     Indicates that the handler was not executed because a previous handler in the chain failed.
    /// </summary>
    Skipped,

    /// <summary>
    ///     Indicates that the handler executed but returned a failure result.
    /// </summary>
    Failed,

    /// <summary>
    ///     Indicates that the handler threw an exception or encountered an unexpected error during execution.
    /// </summary>
    Error
}