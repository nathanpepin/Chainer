using System.Diagnostics;
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
    public async Task<ChainExecutionResult<TContext>> Execute(TContext? context = null, CancellationToken cancellationToken = default)
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

            chainExecutionLog.ExecutedAt = DateTime.UtcNow;
            chainExecutionLog.Status = ChainMessageStatus.Executing;

            logger?.LogInformation("Executing next handler {HandlerName}", handlerName);

            handlerStopWatch.Restart();

            result = (await TryAsync(() => handler.Handle(result.Value, logger, cancellationToken))).Flatten();

            handlerStopWatch.Stop();

            logger?.LogInformation("Handler finished executing in {Elapsed}", handlerStopWatch.Elapsed.ToString("g"));

            chainExecutionLog.FinishedAt = DateTime.UtcNow;

            if (!result.IsFailure)
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
}