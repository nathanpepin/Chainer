namespace Chainer.Persistence;

/// <summary>
///     Defines when context data should be persisted during chain handler execution.
///     Values can be combined using bitwise operations to specify multiple persistence points.
/// </summary>
public enum PersistencePoint
{
    /// <summary>
    ///     Persist context data before the handler executes.
    /// </summary>
    BeforeExecution = 1,

    /// <summary>
    ///     Persist context data after the handler executes.
    /// </summary>
    AfterExecution = 2,

    /// <summary>
    ///     Persist context data both before and after handler execution.
    /// </summary>
    Both = BeforeExecution | AfterExecution
}