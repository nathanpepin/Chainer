using System.Collections.Immutable;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.DependencyInjection;
using static CSharpFunctionalExtensions.Result;


namespace ConsoleApp1;

public interface IInitHandler<TContext>
    where TContext : class, ICloneable, new()
{
    Type[] InitHandlers { get; }
}

public abstract class ChainService<TContext>(IServiceProvider services)
    where TContext : class, ICloneable, new()
{
    protected virtual List<Type> ChainHandlers { get; } = [];
    private List<IChainHandler<TContext>> Handlers { get; } = [];

    public async Task<Result<TContext>> Execute(TContext context, CancellationToken cancellationToken = default)
    {
        if (RegisterHandlers() is (false, _) registration)
            return Failure<TContext>(registration.Error);

        return await new ChainExecutor<TContext>([..Handlers])
            .Execute(context, cancellationToken);
    }

    public async Task<ContextHistoryResult<TContext>> ExecuteWithHistory(TContext context,
        CancellationToken cancellationToken = default)
    {
        var output = new ContextHistoryResult<TContext>();

        if (RegisterHandlers() is (false, _) registration)
        {
            output.Result = Failure<TContext>(registration.Error);
            return output;
        }

        output.Handlers.AddRange(ChainHandlers.Select(x => x.FullName).ToImmutableArray());

        var contextHistoryResult = await new ChainExecutor<TContext>([..Handlers])
            .ExecuteWithHistory(context, cancellationToken);

        output.History.AddRange(contextHistoryResult.History);
        output.UnappliedHandlers.AddRange(output.Handlers[(output.History.Count - 1) ..]);

        if (contextHistoryResult.Result.IsSuccess)
        {
            output.Result = contextHistoryResult.Result;
            return output;
        }

        output.Result = Failure<TContext>(contextHistoryResult.Result.Error);
        return output;
    }

    private (bool Success, string? Error) RegisterHandlers()
    {
        if (Handlers.Count != 0) return (true, null);
        {
            foreach (var it in ChainHandlers)
            {
                var handlerResult = Try(() => services.GetService(it))
                    .Bind(x => x is IChainHandler<TContext> handler
                        ? Success(handler)
                        : Failure<IChainHandler<TContext>>(""));

                if (handlerResult.IsFailure) return (false, $"Handler: {it.FullName} was not registered");

                Handlers.Add(handlerResult.Value);
            }
        }

        return (true, null);
    }
}