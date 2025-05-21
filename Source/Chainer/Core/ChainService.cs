using Chainer.Core.ContextHistory;
using Microsoft.Extensions.Logging;

namespace Chainer.Core;

/// <summary>
///     A dependency injection-friendly base class for creating predefined chains of handlers.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="ChainService{TContext}"/> class provides an abstraction layer that integrates
///         the Chain of Responsibility pattern with dependency injection. It simplifies the creation
///         and execution of chains by:
///         <list type="bullet">
///             <item>Resolving handlers from the DI container</item>
///             <item>Maintaining a predefined sequence of handlers</item>
///             <item>Providing execution methods that mirror <see cref="ChainExecutor{TContext}"/></item>
///             <item>Handling errors in the handler resolution process</item>
///         </list>
///     </para>
///     <para>
///         This abstract class is designed to be extended by creating concrete implementations that
///         define specific chains. The concrete implementation only needs to override the 
///         <see cref="ChainHandlers"/> property to specify which handler types should be
///         included in the chain. The base class handles the rest, including:
///         <list type="bullet">
///             <item>Resolving handler instances from the service provider</item>
///             <item>Validating that all required handlers are registered</item>
///             <item>Creating and configuring a <see cref="ChainExecutor{TContext}"/></item>
///             <item>Executing the chain with appropriate error handling</item>
///         </list>
///     </para>
///     <para>
///         Handlers are resolved lazily upon the first execution, allowing for just-in-time
///         resolution from the DI container. Once resolved, the same handler instances are
///         reused for subsequent executions, ensuring consistent behavior.
///     </para>
///     <para>
///         Example implementation:
///         <code>
///         public class OrderProcessingChain : ChainService&lt;OrderContext&gt;
///         {
///             public OrderProcessingChain(IServiceProvider services, ILogger&lt;OrderProcessingChain&gt; logger)
///                 : base(services, logger)
///             {
///             }
///             
///             protected override List&lt;Type&gt; ChainHandlers { get; } = new()
///             {
///                 typeof(ValidationHandler),
///                 typeof(InventoryCheckHandler),
///                 typeof(PaymentProcessingHandler),
///                 typeof(NotificationHandler)
///             };
///         }
///         </code>
///     </para>
///     <para>
///         Working with a <see cref="ChainService{TContext}"/> involves:
///         <list type="number">
///             <item>Registering all handlers in the DI container</item>
///             <item>Registering the chain service itself in the DI container</item>
///             <item>Injecting the service where chain execution is needed</item>
///             <item>Calling <see cref="Execute"/> or <see cref="ExecuteWithHistory"/> with an appropriate context</item>
///         </list>
///     </para>
///     <para>
///         For more advanced scenarios requiring runtime configuration or dynamic chain
///         composition, consider using <see cref="DynamicChainExecutor"/> instead.
///     </para>
/// </remarks>
/// <typeparam name="TContext">
///     The context type that will flow through the chain. Must be a class that
///     implements <see cref="ICloneable"/> and has a parameterless constructor.
/// </typeparam>
/// <param name="services">
///     The service provider used to resolve handler instances from the DI container.
///     This is typically injected by the DI framework.
/// </param>
/// <param name="logger">
///     A typed logger for logging execution information and errors. This is also
///     typically injected by the DI framework.
/// </param>
public abstract class ChainService<TContext>(IServiceProvider services, ILogger<ChainService<TContext>> logger)
    where TContext : class, ICloneable, new()
{
    /// <summary>
    ///     Defines the types of handlers that make up this chain.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property must be overridden in derived classes to specify the sequence
    ///         of handlers that form the chain. The order of handler types in the list determines
    ///         the execution order when processing a context.
    ///     </para>
    ///     <para>
    ///         Each type in the list should:
    ///         <list type="bullet">
    ///             <item>Be registered with the dependency injection container</item>
    ///             <item>Implement <see cref="IChainHandler{TContext}"/> for the same context type</item>
    ///             <item>Have a constructor compatible with dependency injection</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The base implementation returns an empty list. In a derived class, you would typically
    ///         override this property with a concrete list of handler types:
    ///         <code>
    ///         protected override List&lt;Type&gt; ChainHandlers { get; } = new()
    ///         {
    ///             typeof(FirstHandler),
    ///             typeof(SecondHandler),
    ///             typeof(ThirdHandler)
    ///         };
    ///         </code>
    ///     </para>
    ///     <para>
    ///         If using a source generator or reflection-based registration system, this property
    ///         might be generated or populated through attributes rather than manual definition.
    ///     </para>
    /// </remarks>
    protected virtual List<Type> ChainHandlers { get; } = [];

    /// <summary>
    ///     Collection of resolved handler instances.
    /// </summary>
    /// <remarks>
    ///     This collection stores the resolved handler instances after they are retrieved
    ///     from the dependency injection container. It is populated lazily during the first
    ///     execution and reused for subsequent executions.
    /// </remarks>
    private List<IChainHandler<TContext>> Handlers { get; } = [];

    /// <summary>
    ///     Controls whether logging is enabled for this chain.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property determines whether the injected logger is passed to the
    ///         <see cref="ChainExecutor{TContext}"/> during execution. When set to true (default),
    ///         logging is enabled, and diagnostic information is recorded. When set to false,
    ///         logging is disabled.
    ///     </para>
    ///     <para>
    ///         Override this property in derived classes to control logging behavior:
    ///         <code>
    ///         // Disable logging for performance-critical chains
    ///         protected override bool LoggingEnabled => false;
    ///         </code>
    ///     </para>
    ///     <para>
    ///         This can be useful for:
    ///         <list type="bullet">
    ///             <item>Reducing log volume in high-throughput scenarios</item>
    ///             <item>Improving performance for performance-critical chains</item>
    ///             <item>Implementing environment-specific logging behavior</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     True if logging should be enabled; otherwise, false. The default implementation
    ///     returns true.
    /// </returns>
    protected virtual bool LoggingEnabled => true;

    /// <summary>
    ///     Executes the chain using handlers resolved from dependency injection.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method resolves the handlers specified in <see cref="ChainHandlers"/> from
    ///         the dependency injection container, creates a <see cref="ChainExecutor{TContext}"/>,
    ///         and executes the chain with the provided context.
    ///     </para>
    ///     <para>
    ///         The method performs these steps:
    ///         <list type="number">
    ///             <item>Resolves handler instances from the DI container if not already resolved</item>
    ///             <item>Verifies that all handlers are properly registered and resolved</item>
    ///             <item>Creates a <see cref="ChainExecutor{TContext}"/> with the resolved handlers</item>
    ///             <item>Executes the chain with the provided context</item>
    ///             <item>Returns the result of the chain execution</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         If any handler cannot be resolved from the DI container, the method returns a
    ///         failure result without executing the chain, ensuring that chains are only executed
    ///         when all required handlers are available.
    ///     </para>
    ///     <para>
    ///         This method delegates the actual execution to <see cref="ChainExecutor{TContext}.Execute"/>,
    ///         providing a consistent execution model while handling the DI resolution aspects.
    ///     </para>
    /// </remarks>
    /// <param name="context">
    ///     The context to be processed by the chain. If null, a new context instance will be created.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token for monitoring cancellation requests during execution.
    /// </param>
    /// <returns>
    ///     A <see cref="Result{TContext}"/> containing either the successfully processed context
    ///     or information about the failure if handler resolution or execution failed.
    /// </returns>
    public async Task<Result<TContext>> Execute(TContext? context, CancellationToken cancellationToken = default)
    {
        if (GetRegisteredHandlers() is (false, _) registration)
            return Failure<TContext>(registration.Error ?? "Unknown error");

        return await new ChainExecutor<TContext>([..Handlers], LoggingEnabled ? logger : null)
            .Execute(context, cancellationToken);
    }

    /// <summary>
    ///     Executes the chain with detailed history tracking using handlers resolved from dependency injection.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method extends the basic <see cref="Execute"/> functionality with comprehensive
    ///         history tracking. It resolves handlers from the DI container, creates a
    ///         <see cref="ChainExecutor{TContext}"/>, and executes the chain with history tracking
    ///         enabled.
    ///     </para>
    ///     <para>
    ///         The method captures detailed metadata about the execution process, including:
    ///         <list type="bullet">
    ///             <item>Total execution time for the entire chain</item>
    ///             <item>Individual execution times for each handler</item>
    ///             <item>Success or failure status with detailed error information</item>
    ///             <item>Context state before and after each handler (if context cloning is enabled)</item>
    ///             <item>Handlers that were applied and those that were skipped due to failure</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This method maintains its own execution timing (Start/End timestamps) separate from
    ///         the underlying <see cref="ChainExecutor{TContext}"/>, ensuring that the total time
    ///         includes handler resolution and preparation, not just the execution itself.
    ///     </para>
    ///     <para>
    ///         The returned <see cref="ContextHistoryResult{TContext}"/> includes both the service-level
    ///         metadata and the detailed execution history from the executor, providing a complete
    ///         view of the execution process.
    ///     </para>
    ///     <para>
    ///         If any handler cannot be resolved from the DI container, the method returns a history
    ///         result with a failure status, without attempting to execute the chain.
    ///     </para>
    /// </remarks>
    /// <param name="context">
    ///     The context to be processed by the chain. If null, a new context instance will be created.
    /// </param>
    /// <param name="doNotCloneContext">
    ///     When set to true, the context will not be cloned after each handler execution.
    ///     This improves performance but means the history will not contain the state of
    ///     the context at each step, only references to the final state. Default is false,
    ///     which preserves context state history.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token for monitoring cancellation requests during execution.
    /// </param>
    /// <returns>
    ///     A <see cref="ContextHistoryResult{TContext}"/> containing the execution result,
    ///     timing information, and the history of handler executions and context states.
    /// </returns>
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
            output.Result = Failure<TContext>(registration.Error ?? "Unknown error");
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

    /// <summary>
    ///     Resolves and validates handler instances from the dependency injection container.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This private method handles the resolution of handler instances from the DI container
    ///         based on the types specified in <see cref="ChainHandlers"/>. It ensures that:
    ///         <list type="bullet">
    ///             <item>All required handlers are registered in the DI container</item>
    ///             <item>All resolved services implement the correct handler interface</item>
    ///             <item>Handler instances are cached for reuse across multiple executions</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The method is called lazily during the first execution, and the results are cached
    ///         for subsequent calls, ensuring that handlers are only resolved once.
    ///     </para>
    ///     <para>
    ///         If any handler type cannot be resolved or does not implement the expected interface,
    ///         the method returns a failure result with an error message identifying the problematic
    ///         handler type.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     A tuple containing:
    ///     <list type="bullet">
    ///         <item>A boolean indicating whether all handlers were successfully resolved</item>
    ///         <item>An error message if resolution failed, or null if successful</item>
    ///     </list>
    /// </returns>
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