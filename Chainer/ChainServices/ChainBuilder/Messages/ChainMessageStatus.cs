namespace Chainer.ChainServices.ChainBuilder.Messages;

/// <summary>
///     The state of a chain execution
/// </summary>
public enum ChainMessageStatus
{
    NotStarted = 0,
    Pending,
    Executing,
    Completed,
    Skipped,
    Failed,
    Error
}