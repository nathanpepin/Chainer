using Chainer.Persistence;

namespace Chainer.Abstractions;

public interface IContextPersistence
{
    PersistencePoint PersistWhen { get; }
}