using System.Diagnostics;
using Chainer.Abstractions;
using Chainer.Configuration;
using Chainer.Persistence;
using Microsoft.Extensions.Logging;

namespace Chainer.Execution;

/// <summary>
///     Executes a chain of handlers sequentially, passing context between them and handling errors.
///     This lightweight executor forms the foundation of the Chain of Responsibility pattern implementation.
/// </summary>
public sealed class ChainExecutor<TContext>(IEnumerable<IChainHandler<TContext>>? handlers = null, ILogger? logger = null)
    where TContext : class, ICloneable, new()
{
    private const string NoHandlersErrorMessage = "There were no handlers to execute";
    private List<IChainHandler<TContext>> ChainHandlers { get; } = handlers?.ToList() ?? [];

    /// <summary>
    ///     Adds a handler to the chain's execution sequence.
    /// </summary>
    public ChainExecutor<TContext> AddHandler(IChainHandler<TContext> handler)
    {
        ChainHandlers.Add(handler);
        return this;
    }

    /// <summary>
    ///     Executes the chain of handlers sequentially, passing the context through each handler.
    /// </summary>
    /// <param name="context">
    ///     The context to be processed. If null, a new instance will be created using
    ///     the parameterless constructor of <typeparamref name="TContext" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to monitor for cancellation requests. This token is passed to each
    ///     handler's Handle method, allowing for cooperative cancellation.
    /// </param>
    /// <returns>
    ///     A <see cref="Result{TContext}" /> containing either the successfully processed context
    ///     or information about the failure if any handler failed or threw an exception.
    /// </returns>
    public async Task<ChainExecutionResult<TContext>> ExecuteAsync(TContext? context = null, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Executing chain");

        if (context is null)
            logger?.LogInformation("Context is null, initializing new context");

        context ??= new TContext();
        var result = Result<TContext>.Success(context);

        var executionLogs = ChainHandlers
            .Select(ChainExecutionLog.Create)
            .ToImmutableArray();

        if (ChainHandlers.Count == 0)
        {
            logger?.LogError(NoHandlersErrorMessage);


            return new ChainExecutionResult<TContext>(Result<TContext>.Failure(NoHandlersErrorMessage), executionLogs);
        }

        var queue = new Queue<(IChainHandler<TContext>, ChainExecutionLog)>(ChainHandlers.Zip(executionLogs));

        Stopwatch chainStopWatch = new();
        chainStopWatch.Start();

        Stopwatch handlerStopWatch = new();

        var errorState = false;

        while (queue.Count != 0)
        {
            var (handler, chainExecutionLog) = queue.Dequeue();
            var handlerName = handler.GetType().FullName ?? "Could not get name";

            chainExecutionLog.ExecutedAt = DateTimeOffset.UtcNow;

            if (errorState)
            {
                chainExecutionLog.Status = ChainMessageStatus.Skipped;
                continue;
            }

            SaveBeforeContextDataIfNeeded(chainExecutionLog, result.Value, handler);

            chainExecutionLog.ExecutedAt = DateTime.UtcNow;
            chainExecutionLog.Status = ChainMessageStatus.Executing;

            logger?.LogInformation("Executing next handler {HandlerName}", handlerName);

            handlerStopWatch.Restart();

            result = (await TryAsync(() => handler.Handle(result.Value, logger, cancellationToken))).Flatten();

            handlerStopWatch.Stop();

            logger?.LogInformation("Handler finished executing in {Elapsed}", handlerStopWatch.Elapsed.ToString("g"));

            chainExecutionLog.FinishedAt = DateTime.UtcNow;

            SaveAfterContextDataIfNeeded(chainExecutionLog, result, handler);

            if (result.IsSuccess)
            {
                chainExecutionLog.Status = ChainMessageStatus.Completed;
                chainExecutionLog.FinishedAt = DateTimeOffset.UtcNow;


                continue;
            }

            chainExecutionLog.Status = ChainMessageStatus.Failed;

            errorState = true;

            chainExecutionLog.ErrorMessage = result.Error;
            chainExecutionLog.FinishedAt = DateTimeOffset.UtcNow;

            logger?.LogError("Failed to execute {HandlerName} due to reason {Error}", handlerName, result.Error);
        }

        logger?.LogInformation("Chain executed all handlers in {Elapsed}", chainStopWatch.Elapsed.ToString("g"));

        return new ChainExecutionResult<TContext>(result, executionLogs);
    }

    /// <summary>
    ///     Saves the context after handler execution if the handler implements ISaveAfterContextData.
    /// </summary>
    private static void SaveAfterContextDataIfNeeded(ChainExecutionLog log, Result<TContext> result, object handler)
    {
        if (result.IsSuccess && handler is IContextPersistence persistence && persistence.PersistWhen.HasFlag(PersistencePoint.AfterExecution))
            log.AfterJson = JsonSerializer.Serialize(result.Value);
    }

    /// <summary>
    ///     Saves the context before handler execution if the handler implements ISaveBeforeContextData.
    /// </summary>
    private static void SaveBeforeContextDataIfNeeded(ChainExecutionLog log, TContext context, object handler)
    {
        if (handler is IContextPersistence persistence && persistence.PersistWhen.HasFlag(PersistencePoint.BeforeExecution)) log.BeforeJson = JsonSerializer.Serialize(context);
    }
}