using Chainer.Abstractions;
using Microsoft.Extensions.Logging;

namespace Chainer.Execution;

/// <summary>
///     A dependency injection-friendly base class for creating predefined chains of handlers.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="DependencyInjectedChainExecutor{TContext}" /> class provides an abstraction layer that integrates
///         the Chain of Responsibility pattern with dependency injection. It simplifies the creation
///         and execution of chains by:
///         <list type="bullet">
///             <item>Resolving handlers from the DI container</item>
///             <item>Maintaining a predefined sequence of handlers</item>
///             <item>Providing execution methods that mirror <see cref="ChainExecutor{TContext}" /></item>
///             <item>Handling errors in the handler resolution process</item>
///         </list>
///     </para>
///     <para>
///         This abstract class is designed to be extended by creating concrete implementations that
///         define specific chains. The concrete implementation only needs to override the
///         <see cref="ChainHandlers" /> property to specify which handler types should be
///         included in the chain. The base class handles the rest, including:
///         <list type="bullet">
///             <item>Resolving handler instances from the service provider</item>
///             <item>Validating that all required handlers are registered</item>
///             <item>Creating and configuring a <see cref="ChainExecutor{TContext}" /></item>
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
///         Working with a <see cref="DependencyInjectedChainExecutor{TContext}" /> involves:
///         <list type="number">
///             <item>Registering all handlers in the DI container</item>
///             <item>Registering the chain service itself in the DI container</item>
///             <item>Injecting the service where chain execution is needed</item>
///             <item>Calling <see cref="Execute" /> or <see cref="ExecuteWithHistory" /> with an appropriate context</item>
///         </list>
///     </para>
///     <para>
///         For more advanced scenarios requiring runtime configuration or dynamic chain
///         composition, consider using <see cref="DynamicChainExecutor" /> instead.
///     </para>
/// </remarks>
/// <typeparam name="TContext">
///     The context type that will flow through the chain. Must be a class that
///     implements <see cref="ICloneable" /> and has a parameterless constructor.
/// </typeparam>
/// <param name="services">
///     The service provider used to resolve handler instances from the DI container.
///     This is typically injected by the DI framework.
/// </param>
/// <param name="logger">
///     A typed logger for logging execution information and errors. This is also
///     typically injected by the DI framework.
/// </param>
public abstract class DependencyInjectedChainExecutor<TContext>(IServiceProvider services, ILogger<DependencyInjectedChainExecutor<TContext>> logger)
    where TContext : class, ICloneable, new()
{
    /// <summary>
    ///     Defines the types of handlers that make up this chain.
    /// </summary>
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
    protected virtual bool LoggingEnabled => true;

    /// <summary>
    ///     Executes the chain using handlers resolved from dependency injection.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method resolves the handlers specified in <see cref="ChainHandlers" /> from
    ///         the dependency injection container, creates a <see cref="ChainExecutor{TContext}" />,
    ///         and executes the chain with the provided context.
    ///     </para>
    ///     <para>
    ///         The method performs these steps:
    ///         <list type="number">
    ///             <item>Resolves handler instances from the DI container if not already resolved</item>
    ///             <item>Verifies that all handlers are properly registered and resolved</item>
    ///             <item>Creates a <see cref="ChainExecutor{TContext}" /> with the resolved handlers</item>
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
    ///         This method delegates the actual execution to <see cref="ChainExecutor{TContext}.Execute" />,
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
    ///     A <see cref="Result{TContext}" /> containing either the successfully processed context
    ///     or information about the failure if handler resolution or execution failed.
    /// </returns>
    public async Task<ChainExecutionResult<TContext>> Execute(TContext? context, CancellationToken cancellationToken = default)
    {
        if (GetRegisteredHandlers() is not (false, _) registration)
            return await new ChainExecutor<TContext>([..Handlers], LoggingEnabled ? logger : null)
                .Execute(context, cancellationToken);

        var executionLogs = ChainHandlers
            .Select(ChainExecutionLog.Create<TContext>)
            .ToImmutableArray();

        return new ChainExecutionResult<TContext>(
            Result<TContext>.Failure(registration.Error ?? "Unknown error"),
            executionLogs);
    }

    /// <summary>
    ///     Resolves and validates handler instances from the dependency injection container.
    /// </summary>
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