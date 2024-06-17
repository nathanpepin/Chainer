using CSharpFunctionalExtensions;

namespace ConsoleApp1;

public interface IChainHandler<TContext> where TContext : class, ICloneable, new()
{
    Task<Result<TContext>> Handle(TContext context, CancellationToken cancellationToken = default);
}