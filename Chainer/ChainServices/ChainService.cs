using System.Collections.Immutable;
using Chainer.ChainServices.ContextHistory;
using Microsoft.Extensions.Logging;

namespace Chainer.ChainServices;

/// <summary>
///     A chainer service that executes a predefined chain of handlers via dependency injection.
/// </summary>
/// <param name="services"></param>
/// <param name="logger"></param>
/// <typeparam name="TContext">The context to be acted upon.</typeparam>
public abstract class ChainService<TContext>(IServiceProvider services, ILogger<ChainService<TContext>> logger)
    where TContext : class, ICloneable, new()
{
    /// <summary>
    ///     The chain handlers to execute. Override this property to define the chain of handlers.
    /// </summary>
    protected virtual List<Type> ChainHandlers { get; } = [];

    private List<IChainHandler<TContext>> Handlers { get; } = [];

    /// <summary>
    ///     Override this property to enable or disable logging.
    /// </summary>
    protected virtual bool LoggingEnabled => true;

    /// <summary>
    ///     Gets the registered handlers from the chain handlers and services.
    ///     Safety executes the chain of handlers in sequence or registration and returns the final context result.
    /// </summary>
    /// <param name="context">The context to be acted upon.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<TContext>> Execute(TContext? context, CancellationToken cancellationToken = default)
    {
        if (GetRegisteredHandlers() is (false, _) registration)
            return Failure<TContext>(registration.Error);

        return await new ChainExecutor<TContext>([..Handlers], LoggingEnabled ? logger : null)
            .Execute(context, cancellationToken);
    }

    /// <summary>
    ///     Gets the registered handlers from the chain handlers and services.
    ///     Safety executes the chain of handlers in sequence or registration and returns the final context result.
    /// </summary>
    /// <param name="context">The context to be acted upon</param>
    /// <param name="doNotCloneContext">
    ///     If set to true, the context history will be set to the result.
    ///     Otherwise, it will store a copy of the context state at each execution step.
    /// </param>
    /// ///
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ContextHistoryResult<TContext>> ExecuteWithHistory(TContext? context = null,
        bool doNotCloneContext = false,
        CancellationToken cancellationToken = default)
    {
        context ??= new TContext();

        var output = new ContextHistoryResult<TContext>
        {
            Start = DateTime.UtcNow
        };

        if (GetRegisteredHandlers() is (false, _) registration)
        {
            output.End = DateTime.UtcNow;
            output.Result = Failure<TContext>(registration.Error);
            return output;
        }

        output.Handlers.AddRange(ChainHandlers.Select(x => x.FullName).ToImmutableArray());

        var contextHistoryResult = await new ChainExecutor<TContext>([..Handlers], LoggingEnabled ? logger : null)
            .ExecuteWithHistory(context, doNotCloneContext, cancellationToken);

        output.History.AddRange(contextHistoryResult.History);
        output.UnappliedHandlers.AddRange(output.Handlers[output.History.Count ..]);
        output.End = DateTime.UtcNow;

        if (contextHistoryResult.Result.IsSuccess)
        {
            output.Result = contextHistoryResult.Result;
            return output;
        }

        output.Result = Failure<TContext>(contextHistoryResult.Result.Error);
        return output;
    }

    private (bool Success, string? Error) GetRegisteredHandlers()
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