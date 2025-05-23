namespace Chainer.Persistence;

public enum PersistencePoint
{
    BeforeExecution = 1,
    AfterExecution = 2,
    Both = BeforeExecution | AfterExecution
}