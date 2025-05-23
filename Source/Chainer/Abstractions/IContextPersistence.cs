using Chainer.Persistence;

namespace Chainer.Abstractions;

/// <summary>
/// Defines when context data should be persisted during chain handler execution.
/// Handlers implementing this interface can specify whether context should be serialized
/// to JSON before execution, after execution, or both for auditing and debugging purposes.
/// </summary>
public interface IContextPersistence
{
    /// <summary>
    /// Gets when the context should be persisted during handler execution.
    /// </summary>
    PersistencePoint PersistWhen { get; }
}