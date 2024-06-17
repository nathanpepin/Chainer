using CSharpFunctionalExtensions;
using Microsoft.Extensions.DependencyInjection;
using static CSharpFunctionalExtensions.Result;

namespace ConsoleApp1;

public abstract class ChainInOutService<TContext, TIn, TOut>(IServiceProvider services)
    where TContext : class, ICloneable, new()
{
    protected abstract List<Type> ChainHandlers { get; }
    protected abstract Func<TIn, Task<TContext>> Import { get; }
    protected abstract Func<TContext, Task<TOut>> Export { get; }
    private List<IChainHandler<TContext>> Handlers { get; } = [];

    public async Task<Result<TOut>> Execute(TIn input, CancellationToken cancellationToken = default)
    {
        if (RegisterHandlers() is (false, _) registration)
            return Failure<TOut>(registration.Error);

        Queue<IChainHandler<TContext>> chainHandlers = new(Handlers);

        return await Result
            .Try(() => Import(input))
            .BindTry(x => new ChainExecutor<TContext>(chainHandlers).Execute(x, cancellationToken))
            .MapTry(Export);
    }

    public async Task<ContextHistoryResult<TContext, TOut>> ExecuteWithHistory(TIn input,
        CancellationToken cancellationToken = default)
    {
        var output = new ContextHistoryResult<TContext, TOut>();

        if (RegisterHandlers() is (false, _) registration)
        {
            output.Result = Failure<TOut>(registration.Error);
            return output;
        }

        var import = await Try(() => Import(input));

        if (import.IsFailure)
        {
            output.Result = Failure<TOut>(import.Error);
            return output;
        }

        var contextHistoryResult = await new ChainExecutor<TContext>([..Handlers])
            .ExecuteWithHistory(import.Value, cancellationToken);

        output.History.AddRange(contextHistoryResult.History);

        if (contextHistoryResult.Result.IsFailure)
        {
            output.Result = Failure<TOut>(contextHistoryResult.Result.Error);
            return output;
        }

        output.Result = await Try<TOut>(() => Export(contextHistoryResult.Result.Value));

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