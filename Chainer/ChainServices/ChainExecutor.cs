using System.Collections.Immutable;
using System.Diagnostics;
using Chainer.Calculation;
using Chainer.ChainServices.ContextHistory;
using Microsoft.Extensions.Logging;

namespace Chainer.ChainServices;

/// <summary>
///     Chain executor that executes a chain of handlers by piping the results of each handler to the next one.
///     If a chain handler fails for any caught or uncaught reason, the chain stops executing and returns the error.
/// </summary>
/// <param name="handlers">The handlers to execute. Can also use the AddHandler() method for fluent addition.</param>
/// <param name="logger">The logger if wanted. The logger is passed down to each handler.</param>
/// <typeparam name="TContext">The context to be acted upon.</typeparam>
public sealed class ChainExecutor<TContext>(IEnumerable<IChainHandler<TContext>>? handlers = null, ILogger? logger = null)
    where TContext : class, ICloneable, new()
{
    private const string NoHandlersErrorMessage = "There were no handlers to execute";
    private List<IChainHandler<TContext>> ChainHandlers { get; } = handlers?.ToList() ?? [];

    /// <summary>
    ///     Adds a handler to the chain.
    /// </summary>
    /// <param name="handler"></param>
    /// <returns></returns>
    public ChainExecutor<TContext> AddHandler(IChainHandler<TContext> handler)
    {
        ChainHandlers.Add(handler);
        return this;
    }

    /// <summary>
    ///     Safety executes the chain of handlers in sequence or registration and returns the final context result.
    /// </summary>
    /// <param name="context">The context to be acted upon.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

            var result = await Try(() => handler.Handle(context, logger, cancellationToken));

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

    /// <summary>
    ///     Safety executes the chain of handlers in sequence or registration and returns the final context result.
    ///     Provides metadata about the execution such as the start and end time, the handlers that were executed, and the history of the context.
    /// </summary>
    /// <param name="context">The context to be acted upon.</param>
    /// <param name="doNotCloneContext">
    ///     If set to true, the context history will be set to the result.
    ///     Otherwise, it will store a copy of the context state at each execution step.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

            var result = await Try(() => handler.Handle(context, logger, cancellationToken));

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