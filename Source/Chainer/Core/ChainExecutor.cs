using System.Diagnostics;
using Chainer.Core.ContextHistory;
using Microsoft.Extensions.Logging;

namespace Chainer.Core;

/// <summary>
///     Executes a chain of handlers sequentially, passing context between them and handling errors.
///     This lightweight executor forms the foundation of the Chain of Responsibility pattern implementation.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="ChainExecutor{TContext}"/> class serves as the primary execution engine for
///         running a sequence of handlers that process a shared context. It manages the entire lifecycle
///         of chain execution, including:
///         <list type="bullet">
///             <item>Sequential handler execution in registration order</item>
///             <item>Context initialization and passing between handlers</item>
///             <item>Error handling and propagation from any point in the chain</item>
///             <item>Performance tracking and logging for diagnostics</item>
///             <item>Support for both basic execution and detailed history collection</item>
///         </list>
///     </para>
///     <para>
///         The executor operates on a "fail-fast" principle: if any handler in the chain returns a
///         failure result or throws an exception, execution immediately stops and the error is
///         returned. This behavior ensures that invalid states don't propagate through the chain
///         and simplifies error handling for consumers.
///     </para>
///     <para>
///         Two execution modes are available:
///         <list type="bullet">
///             <item>
///                 <see cref="Execute"/> - Processes the context through all handlers and returns
///                 the final result. This is ideal for simple processing needs.
///             </item>
///             <item>
///                 <see cref="ExecuteWithHistory"/> - Additionally tracks execution details including
///                 timing, context state changes, and execution flow. This provides rich metadata
///                 for auditing, debugging, and analysis.
///             </item>
///         </list>
///     </para>
///     <para>
///         Handlers can be provided during construction or added incrementally using the fluent
///         <see cref="AddHandler"/> method. The executor creates a new context instance automatically
///         if none is provided, allowing chains to initialize their own processing context.
///     </para>
///     <para>
///         Example usage:
///         <code>
///         // Create with constructor
///         var executor = new ChainExecutor&lt;OrderContext&gt;([new ValidationHandler(), new ProcessingHandler()]);
///         
///         // Or build with fluent API
///         var executor = new ChainExecutor&lt;OrderContext&gt;()
///             .AddHandler(new ValidationHandler())
///             .AddHandler(new ProcessingHandler());
///             
///         // Execute with a new context
///         var result = await executor.Execute(new OrderContext { OrderId = 123 });
///         
///         // Or execute with detailed history
///         var historyResult = await executor.ExecuteWithHistory(orderContext);
///         Console.WriteLine($"Chain execution took {historyResult.ExecutionTime}");
///         </code>
///     </para>
///     <para>
///         For more advanced scenarios requiring dynamic configuration or persistence,
///         consider using <see cref="DynamicChainExecutor"/> instead.
///     </para>
/// </remarks>
/// <typeparam name="TContext">
///     The type of context that flows through the chain. Must be a class that implements
///     <see cref="ICloneable"/> and has a parameterless constructor.
/// </typeparam>
/// <param name="handlers">
///     Optional collection of handlers to initialize the chain. If null, an empty chain
///     is created, and handlers can be added using <see cref="AddHandler"/>.
/// </param>
/// <param name="logger">
///     Optional logger for diagnostic information. When provided, the logger is passed
///     to each handler's <see cref="IChainHandler{TContext}.Handle"/> method and is used
///     for executor-level logging.
/// </param>
public sealed class ChainExecutor<TContext>(IEnumerable<IChainHandler<TContext>>? handlers = null, ILogger? logger = null)
    where TContext : class, ICloneable, new()
{
    private const string NoHandlersErrorMessage = "There were no handlers to execute";
    private List<IChainHandler<TContext>> ChainHandlers { get; } = handlers?.ToList() ?? [];

    /// <summary>
    ///     Adds a handler to the chain's execution sequence.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method appends a handler to the end of the current execution sequence.
    ///         Handlers are executed in the order they are added, with each receiving the
    ///         context as modified by previous handlers.
    ///     </para>
    ///     <para>
    ///         The method supports a fluent interface pattern, allowing for method chaining
    ///         to build complex handler sequences in a readable manner:
    ///         <code>
    ///         var executor = new ChainExecutor&lt;OrderContext&gt;()
    ///             .AddHandler(new ValidationHandler())
    ///             .AddHandler(new PricingHandler())
    ///             .AddHandler(new NotificationHandler());
    ///         </code>
    ///     </para>
    ///     <para>
    ///         Handlers can be added at any time, even after previous executions, to reuse
    ///         and extend existing chains.
    ///     </para>
    /// </remarks>
    /// <param name="handler">
    ///     The handler to add to the chain. Must implement <see cref="IChainHandler{TContext}"/>
    ///     for the same context type as the executor.
    /// </param>
    /// <returns>
    ///     The current <see cref="ChainExecutor{TContext}"/> instance, enabling method chaining.
    /// </returns>
    public ChainExecutor<TContext> AddHandler(IChainHandler<TContext> handler)
    {
        ChainHandlers.Add(handler);
        return this;
    }

    /// <summary>
    ///     Executes the chain of handlers sequentially, passing the context through each handler.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method processes the provided context (or creates a new one if none is provided)
    ///         through each handler in the chain sequentially. Each handler can modify the context
    ///         before passing it to the next handler in the sequence.
    ///     </para>
    ///     <para>
    ///         The execution follows these steps:
    ///         <list type="number">
    ///             <item>Initialize or validate the input context</item>
    ///             <item>Verify that at least one handler exists</item>
    ///             <item>For each handler in sequence:
    ///                 <list type="bullet">
    ///                     <item>Call the handler's Handle method with the current context</item>
    ///                     <item>If the handler returns a failure result, stop execution and return the failure</item>
    ///                     <item>If the handler throws an exception, capture it and return as a failure</item>
    ///                     <item>If successful, continue to the next handler</item>
    ///                 </list>
    ///             </item>
    ///             <item>Return the final context if all handlers complete successfully</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Performance metrics are tracked and logged (if a logger is provided) for
    ///         both the overall chain and individual handlers.
    ///     </para>
    ///     <para>
    ///         This method is ideal for standard processing flows where detailed execution
    ///         history is not required. For scenarios requiring execution history and metadata,
    ///         use <see cref="ExecuteWithHistory"/> instead.
    ///     </para>
    /// </remarks>
    /// <param name="context">
    ///     The context to be processed. If null, a new instance will be created using
    ///     the parameterless constructor of <typeparamref name="TContext"/>.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to monitor for cancellation requests. This token is passed to each
    ///     handler's Handle method, allowing for cooperative cancellation.
    /// </param>
    /// <returns>
    ///     A <see cref="Result{TContext}"/> containing either the successfully processed context
    ///     or information about the failure if any handler failed or threw an exception.
    /// </returns>
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

            var result = await TryAsync(() => handler.Handle(context, logger, cancellationToken));

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
    ///     Executes the chain with detailed history tracking, capturing timing and context state for each handler.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method extends the basic execution functionality with comprehensive history
    ///         tracking. It captures detailed metadata about the execution process, including:
    ///         <list type="bullet">
    ///             <item>Total execution time for the entire chain</item>
    ///             <item>Individual execution times for each handler</item>
    ///             <item>Success or failure status with detailed error information</item>
    ///             <item>Context state before and after each handler (if context cloning is enabled)</item>
    ///             <item>Handlers that were applied and those that were skipped due to failure</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         By default, this method captures the context state at each step by calling
    ///         <see cref="ICloneable.Clone"/> on the context after each handler completes.
    ///         This provides a complete audit trail of how the context changed during processing.
    ///         For performance-sensitive scenarios or large context objects, context cloning
    ///         can be disabled with the <paramref name="doNotCloneContext"/> parameter.
    ///     </para>
    ///     <para>
    ///         The execution follows the same fail-fast principle as <see cref="Execute"/>:
    ///         if any handler fails, execution stops immediately, and remaining handlers are
    ///         marked as "unapplied" in the history.
    ///     </para>
    ///     <para>
    ///         The returned <see cref="ContextHistoryResult{TContext}"/> provides rich
    ///         information for debugging, performance analysis, and auditing. It can be
    ///         logged, stored, or displayed to give insight into the execution process.
    ///     </para>
    ///     <para>
    ///         Example usage:
    ///         <code>
    ///         var result = await executor.ExecuteWithHistory(orderContext);
    ///         
    ///         // Log the execution summary
    ///         Console.WriteLine(result.PrintOutput());
    ///         
    ///         // Analyze handler performance
    ///         var slowestHandler = result.History
    ///             .OrderByDescending(h => h.Duration)
    ///             .FirstOrDefault();
    ///         
    ///         if (slowestHandler != null)
    ///         {
    ///             Console.WriteLine($"Slowest handler: {slowestHandler.Handler}, " +
    ///                             $"Duration: {slowestHandler.Duration}");
    ///         }
    ///         </code>
    ///     </para>
    /// </remarks>
    /// <param name="context">
    ///     The context to be processed. If null, a new instance will be created using
    ///     the parameterless constructor of <typeparamref name="TContext"/>.
    /// </param>
    /// <param name="doNotCloneContext">
    ///     When set to true, the context will not be cloned after each handler execution.
    ///     This improves performance but means the history will not contain the state of
    ///     the context at each step, only references to the final state. Default is false,
    ///     which preserves context state history.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to monitor for cancellation requests. This token is passed to each
    ///     handler's Handle method, allowing for cooperative cancellation.
    /// </param>
    /// <returns>
    ///     A <see cref="ContextHistoryResult{TContext}"/> containing the execution result,
    ///     timing information, and the history of handler executions and context states.
    /// </returns>
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

            var result = await TryAsync(() => handler.Handle(context, logger, cancellationToken));

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