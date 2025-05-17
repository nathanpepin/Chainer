namespace Chainer.ChainServices.ChainBuilder.Messages;

public enum ChainMessageStatus
{
    NotStarted = 0,
    Pending,
    Executing,
    Completed,
    Failed,
    Skipped
}