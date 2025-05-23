using Chainer.Abstractions;
using Microsoft.Extensions.Logging;

namespace Chainer.Execution;

/// <summary>
///     A dependency injection-friendly base class for creating predefined chains of handlers.
/// </summary>
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
    private List<IChainHandler<TContext>> Handlers { get; } = [];

    /// <summary>
    ///     Controls whether logging is enabled for this chain.
    /// </summary>
    protected virtual bool LoggingEnabled => true;

    /// <summary>
    ///     Executes the chain using handlers resolved from dependency injection.
    /// </summary>
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
    public async Task<ChainExecutionResult<TContext>> ExecuteAsync(TContext? context, CancellationToken cancellationToken = default)
    {
        if (GetRegisteredHandlers() is not (false, _) registration)
            return await new ChainExecutor<TContext>([..Handlers], LoggingEnabled ? logger : null)
                .ExecuteAsync(context, cancellationToken);

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