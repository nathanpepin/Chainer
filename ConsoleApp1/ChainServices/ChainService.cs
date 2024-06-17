using CSharpFunctionalExtensions;
using Microsoft.Extensions.DependencyInjection;
using static CSharpFunctionalExtensions.Result;


namespace ConsoleApp1;

public abstract class ChainService<TContext>(IServiceProvider services)
    where TContext : class, ICloneable, new()
{
    protected abstract List<Type> ChainHandlers { get; }
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

        var contextHistoryResult = await new ChainExecutor<TContext>([..Handlers])
            .ExecuteWithHistory(context, cancellationToken);

        output.History.AddRange(contextHistoryResult.History);

        if (!contextHistoryResult.Result.IsFailure)
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
            var registeredHandlers = services
                .GetServices<IChainHandler<TContext>>()
                .Join(ChainHandlers, handler => handler.GetType().FullName, type => type.FullName,
                    (handler, type) => (type, handler))
                .ToDictionary(x => x.type, x => x.handler);

            foreach (var it in ChainHandlers)
            {
                if (!registeredHandlers.TryGetValue(it, out var handler))
                {
                    return (false, $"Handler: {it.FullName} was not registered");
                }

                Handlers.Add(handler);
            }
        }

        return (true, null);
    }
}