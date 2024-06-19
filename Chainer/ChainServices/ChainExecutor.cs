using System.Collections.Immutable;
using System.Diagnostics;
using Chainer.Calculation;
using Chainer.ChainServices.ContextHistory;
using Microsoft.Extensions.Logging;

namespace Chainer.ChainServices;

public sealed class ChainExecutor<TContext>(IEnumerable<IChainHandler<TContext>>? handlers = null, ILogger? logger = null)
    where TContext : class, ICloneable, new()
{
    private List<IChainHandler<TContext>> ChainHandlers { get; } = handlers?.ToList() ?? [];

    private const string NoHandlersErrorMessage = "There were no handlers to execute";

    public ChainExecutor<TContext> AddHandler(IChainHandler<TContext> handler)
    {
        ChainHandlers.Add(handler);
        return this;
    }

    public async Task<Result<TContext>> Execute(TContext? context = null, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Executing chain");

        if (context is null)
            logger?.LogInformation("Context is null, initializing new context");

        context ??= new TContext();

        if (ChainHandlers.Count == 0)
        {
            logger?.LogError(NoHandlersErrorMessage);
            return Failure<TContext>(NoHandlersErrorMessage);
        }

        var queue = new Queue<IChainHandler<TContext>>(ChainHandlers);

        Stopwatch chainStopWatch = new();
        chainStopWatch.Start();
        
        Stopwatch handlerStopWatch = new();

        while (queue.Count != 0)
        {
            var handler = queue.Dequeue();
            var handlerName = handler.GetType().FullName ?? "Could not get name";

            logger?.LogInformation("Executing next handler {HandlerName}", handlerName);

            handlerStopWatch.Restart();

            var result = await Try(() => handler.Handle(context, cancellationToken));

            handlerStopWatch.Stop();

            logger?.LogInformation("Handler finished executing in {Elapsed}", handlerStopWatch.Elapsed.ToString("g"));

            var flattenedResult = result.Flatten();

            if (!flattenedResult.IsFailure) continue;

            logger?.LogError("Failed to execute {HandlerName} due to reason {Error}", handlerName, flattenedResult.Error);
            return flattenedResult;
        }

        logger?.LogInformation("Chain executed all handlers in {Elapsed}", chainStopWatch.Elapsed.ToString("g"));

        return context;
    }

    public async Task<ContextHistoryResult<TContext>> ExecuteWithHistory(TContext? context,
        bool doNotCloneContext = false,
        CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Executing chain (with history)");

        var output = new ContextHistoryResult<TContext>
        {
            Start = DateTime.UtcNow
        };

        if (context is null)
            logger?.LogInformation("Context is null, initializing new context");

        context ??= new TContext();

        if (ChainHandlers.Count == 0)
        {
            logger?.LogWarning(NoHandlersErrorMessage);
            output.Result = Failure<TContext>(NoHandlersErrorMessage);
            output.End = DateTime.Now;
            return output;
        }

        var handlerNames = ChainHandlers
            .Select(x => x.GetType().FullName ?? "Could not get name")
            .ToImmutableArray();
        output.Handlers.AddRange(handlerNames);

        var queue = new Queue<IChainHandler<TContext>>(ChainHandlers);

        Stopwatch chainStopWatch = new();
        chainStopWatch.Start();
        
        Stopwatch handlerStopWatch = new();

        while (queue.Count != 0)
        {
            var handler = queue.Dequeue();
            var handlerName = handler.GetType().FullName ?? "Could not get name";

            logger?.LogInformation("Executing next handler {HandlerName}", handlerName);

            var start = DateTime.UtcNow;

            handlerStopWatch.Restart();

            var result = await Try(() => handler.Handle(context, cancellationToken));

            handlerStopWatch.Stop();

            logger?.LogInformation("Handler finished executing in {Elapsed}", handlerStopWatch.Elapsed.ToString("g"));

            var flattenedResult = result.Flatten();
            output.Result = flattenedResult;

            if (flattenedResult.IsFailure)
            {
                logger?.LogError("Failed to execute {HandlerName} due to reason {Error}", handlerName, flattenedResult.Error);

                output.UnappliedHandlers.Add(handlerName);

                var unappliedHandlerNames = queue
                    .Select(x => x.GetType().FullName ?? "Could not get name")
                    .ToImmutableArray();
                output.UnappliedHandlers.AddRange(unappliedHandlerNames);

                output.End = DateTime.UtcNow;

                return output;
            }

            output.History.Add(new HandlerResult<TContext>(
                handler.GetType().FullName ?? "Could not get name",
                doNotCloneContext ? context : (TContext)context.Clone(),
                start,
                DateTime.UtcNow));
        }

        logger?.LogInformation("Chain (with history) executed all handlers in {Elapsed}", chainStopWatch.Elapsed.ToString("g"));

        output.End = DateTime.UtcNow;
        return output;
    }
}