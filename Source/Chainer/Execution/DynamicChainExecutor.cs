using Chainer.Abstractions;
using Chainer.Configuration;
using Chainer.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chainer.Execution;

/// <summary>
///     Executes chains of handlers dynamically based on configuration retrieved from a repository.
/// </summary>
/// <remarks>
///     DynamicChainExecutor is the core execution engine for the dynamic chain system. It:
///     <list type="bullet">
///         <item>Retrieves chain configurations from a repository</item>
///         <item>Creates and configures handler instances</item>
///         <item>Executes handlers in sequence, passing context between them</item>
///         <item>Tracks execution status and logs for each handler</item>
///         <item>Manages error handling and recovery</item>
///     </list>
///     This executor provides a flexible way to define and execute processing pipelines
///     where both the handlers and their configurations can be determined at runtime
///     through various sources (databases, configuration files, etc.).
///     Unlike static ChainExecutor, this class supports:
///     <list type="bullet">
///         <item>Named chains that can be looked up by ID or friendly name</item>
///         <item>Storing execution logs for auditing and debugging</item>
///         <item>Handler configuration through various formats (JSON, object, etc.)</item>
///         <item>Dynamic handler resolution and instantiation</item>
///     </list>
/// </remarks>
/// <param name="repository">The repository for retrieving chain configurations and storing execution logs</param>
/// <param name="serviceProvider">The service provider for resolving handler dependencies</param>
/// <param name="logger">The logger for recording execution information</param>
public sealed class DynamicChainExecutor(
    IChainRepository repository,
    IServiceProvider serviceProvider,
    ILogger<DynamicChainExecutor> logger) : IDynamicChainExecutor
{
    /// <summary>
    ///     Cache of handler types to improve performance by avoiding repeated Type.GetType calls.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Type?> TypeCache = new();

    /// <summary>
    ///     Executes the default chain for the specified context type.
    /// </summary>
    /// <typeparam name="TContext">The type of context to process</typeparam>
    /// <param name="initialContext">The initial context to process, or null to create a new instance</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>A result containing the processed context and execution logs</returns>
    /// <remarks>
    ///     The default chain is identified by InMemoryChainRepository.DefaultChainGuid,
    ///     which is a predefined GUID used as a standard identifier for the default chain.
    ///     This method is a convenience wrapper around ExecuteChainAsync(Guid, TContext, CancellationToken).
    /// </remarks>
    public Task<ChainExecutionResult<TContext>> ExecuteDefaultChainAsync<TContext>(TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new()
    {
        return ExecuteChainAsync(InMemoryChainRepository.DefaultChainGuid, initialContext, cancellationToken);
    }

    /// <summary>
    ///     Executes a chain identified by a friendly name for the specified context type.
    /// </summary>
    /// <typeparam name="TContext">The type of context to process</typeparam>
    /// <param name="friendlyName">The friendly name of the chain to execute</param>
    /// <param name="initialContext">The initial context to process, or null to create a new instance</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>A result containing the processed context and execution logs</returns>
    /// <remarks>
    ///     The friendly name is converted to a deterministic GUID using GuidFromString,
    ///     which ensures that the same name always maps to the same GUID.
    ///     This allows chains to be referenced by human-readable names rather than GUIDs,
    ///     while still maintaining consistent identification across systems.
    ///     This method is a convenience wrapper around ExecuteChainAsync(Guid, TContext, CancellationToken).
    /// </remarks>
    public Task<ChainExecutionResult<TContext>> ExecuteChainAsync<TContext>(string friendlyName, TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new()
    {
        var chainId = GuidFromString.CreateDeterministicGuid(friendlyName);
        return ExecuteChainAsync(chainId, initialContext, cancellationToken);
    }

    /// <summary>
    ///     Executes a chain identified by its GUID for the specified context type.
    /// </summary>
    /// <typeparam name="TContext">The type of context to process</typeparam>
    /// <param name="chainId">The unique identifier of the chain to execute</param>
    /// <param name="initialContext">The initial context to process, or null to create a new instance</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>A result containing the processed context and execution logs</returns>
    /// <remarks>
    ///     This method:
    ///     <list type="number">
    ///         <item>Retrieves the chain configuration from the repository using the chainId</item>
    ///         <item>Returns a failure result if the chain configuration cannot be retrieved</item>
    ///         <item>
    ///             Delegates to ExecuteChainAsync(IEnumerable&lt;ChainMessage&gt;, TContext, CancellationToken) for
    ///             execution
    ///         </item>
    ///     </list>
    /// </remarks>
    public async Task<ChainExecutionResult<TContext>> ExecuteChainAsync<TContext>(
        Guid chainId,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new()
    {
        // Get chain messages
        var messagesResult = await repository.GetChainMessagesAsync(chainId, cancellationToken);
        if (messagesResult.IsFailure) return new ChainExecutionResult<TContext>(Failure<TContext>(messagesResult.Error), []);

        return await ExecuteChainAsync(messagesResult.Value, initialContext, cancellationToken);
    }

    /// <summary>
    ///     Executes a chain defined by a collection of ChainMessage objects for the specified context type.
    /// </summary>
    /// <typeparam name="TContext">The type of context to process</typeparam>
    /// <param name="chainMessages">The collection of ChainMessage objects defining the chain</param>
    /// <param name="initialContext">The initial context to process, or null to create a new instance</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>A result containing the processed context and execution logs</returns>
    /// <remarks>
    ///     This is the core execution method that:
    ///     <list type="number">
    ///         <item>Prepares the chain messages for execution by sorting and wrapping them</item>
    ///         <item>Creates a new context instance if initialContext is null</item>
    ///         <item>Executes the chain handlers in order, passing the context between them</item>
    ///         <item>Collects execution logs for each handler</item>
    ///         <item>Returns a composite result with both the final context and execution logs</item>
    ///     </list>
    ///     This method allows for in-memory chain execution without requiring the chain
    ///     to be pre-registered in a repository, which is useful for ad-hoc or dynamically
    ///     generated chains.
    /// </remarks>
    public async Task<ChainExecutionResult<TContext>> ExecuteChainAsync<TContext>(
        IEnumerable<ChainMessage> chainMessages,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new()
    {
        var orderedMessages = PrepareOrderedExecutionItems(chainMessages);
        var context = initialContext ?? new TContext();

        var (success, resultContext, errorMessage) = await ExecuteChainMessagesInOrderAsync(
            orderedMessages,
            context,
            cancellationToken);

        var contextResult = success
            ? Success(resultContext)
            : Failure<TContext>(errorMessage);

        var messages = orderedMessages
            .Select(x => x.ExecutionLog)
            .ToImmutableArray();

        return new ChainExecutionResult<TContext>(contextResult, messages);
    }

    /// <summary>
    ///     Prepares chain messages for execution by ordering them and creating execution items.
    /// </summary>
    private static ImmutableArray<ExecutionItem> PrepareOrderedExecutionItems(IEnumerable<ChainMessage> chainMessages)
    {
        return
        [
            ..chainMessages
                .OrderBy(m => m.ExecutionOrder)
                .Select(x => new ExecutionItem(x, new ChainExecutionLog(x)))
        ];
    }

    /// <summary>
    ///     Executes chain messages in order, passing the context between them and tracking execution status.
    /// </summary>
    private async Task<(bool success, TContext context, string errorMessage)> ExecuteChainMessagesInOrderAsync<TContext>(
        ImmutableArray<ExecutionItem> executionItems,
        TContext context,
        CancellationToken cancellationToken)
        where TContext : class, ICloneable, new()
    {
        var errorState = false;
        var errorMessage = string.Empty;

        await repository.SaveChainExecutionLogs(executionItems.Select(x => x.ExecutionLog), cancellationToken);

        foreach (var item in executionItems)
        {
            if (errorState)
            {
                await MarkMessageSkipped(item.ExecutionLog, cancellationToken);
                continue;
            }

            var result = await ExecuteSingleMessageAsync(item, context, cancellationToken);

            if (!result.IsFailure) continue;

            errorState = true;
            errorMessage = result.Error;
        }

        return (success: !errorState, context, errorMessage);
    }

    /// <summary>
    ///     Executes a single chain message and updates its execution log.
    /// </summary>
    private async Task<Result<TContext>> ExecuteSingleMessageAsync<TContext>(
        ExecutionItem item,
        TContext context,
        CancellationToken cancellationToken)
        where TContext : class, ICloneable, new()
    {
        var message = item.Message;
        var log = item.ExecutionLog;

        try
        {
            log.ExecutedAt = DateTimeOffset.UtcNow;
            await repository.UpdateChainExecutionLog(log, ChainMessageStatus.Executing, cancellationToken);

            var handlerResult = await GetAndExecuteHandlerAsync(message, context, log, cancellationToken);

            if (handlerResult.IsFailure)
            {
                log.FinishedAt = DateTimeOffset.UtcNow;
                await repository.UpdateChainExecutionLog(log, ChainMessageStatus.Failed, cancellationToken);
                return handlerResult;
            }

            log.FinishedAt = DateTimeOffset.UtcNow;
            await repository.UpdateChainExecutionLog(log, ChainMessageStatus.Completed, cancellationToken);
            return handlerResult;
        }
        catch (Exception exception)
        {
            log.FinishedAt = DateTimeOffset.UtcNow;
            await repository.UpdateChainExecutionLog(log, ChainMessageStatus.Error, cancellationToken);

            return Failure<TContext>(exception.Message);
        }
    }

    /// <summary>
    ///     Retrieves, configures, and executes a chain handler according to its message.
    /// </summary>
    private async Task<Result<TContext>> GetAndExecuteHandlerAsync<TContext>(
        ChainMessage message,
        TContext context,
        ChainExecutionLog log,
        CancellationToken cancellationToken)
        where TContext : class, ICloneable, new()
    {
        // Get handler type
        var handlerType = TypeCache.GetOrAdd(message.HandlerTypeName, Type.GetType(message.HandlerTypeName));
        if (handlerType is null) return Failure<TContext>($"Handler type {message.HandlerTypeName} not found");

        // Get handler instance from DI or create new
        var handler = serviceProvider.GetService(handlerType) ??
                      ActivatorUtilities.CreateInstance(serviceProvider, handlerType);

        if (handler is not IChainHandler<TContext> typedHandler)
            return Failure<TContext>($"Handler {handlerType.Name} does not implement IChainHandler<{typeof(TContext).Name}>");

        await SaveBeforeContextDataIfNeeded(handler, context, log);
        ConfigureHandlerIfNeeded<TContext>(handler, message);

        var result = await typedHandler.Handle(context, logger, cancellationToken);

        SaveAfterContextDataIfNeeded(log, result, handler);

        return result;
    }

    /// <summary>
    ///     Saves the context after handler execution if the handler implements ISaveAfterContextData.
    /// </summary>
    private static void SaveAfterContextDataIfNeeded<TContext>(ChainExecutionLog log, Result<TContext> result, object handler)
        where TContext : class, ICloneable, new()
    {
        if (result.IsSuccess && handler is IContextPersistence persistence && persistence.PersistWhen.HasFlag(PersistencePoint.AfterExecution))
            log.AfterJson = JsonSerializer.Serialize(result.Value);
    }

    /// <summary>
    ///     Saves the context before handler execution if the handler implements ISaveBeforeContextData.
    /// </summary>
    private static Task SaveBeforeContextDataIfNeeded<TContext>(object handler, TContext context, ChainExecutionLog log)
        where TContext : class, ICloneable, new()
    {
        if (handler is IContextPersistence persistence && persistence.PersistWhen.HasFlag(PersistencePoint.AfterExecution)) log.BeforeJson = JsonSerializer.Serialize(context);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Configures a handler with its message configuration if it supports configuration.
    /// </summary>
    private static void ConfigureHandlerIfNeeded<TContext>(
        object handler,
        ChainMessage message)
        where TContext : class, ICloneable, new()
    {
        if (handler is not IConfigurableChainHandler<TContext> configurableHandler || string.IsNullOrEmpty(message.Configuration)) return;

        var configuration = ChainHandlerConfiguration.FromJson(message.Configuration);
        configurableHandler.Configure(configuration);
    }

    /// <summary>
    ///     Marks a message as skipped in the repository.
    /// </summary>
    private Task<Result> MarkMessageSkipped(ChainExecutionLog log, CancellationToken cancellationToken)
    {
        return repository.UpdateChainExecutionLog(log, ChainMessageStatus.Skipped, cancellationToken);
    }

    /// <summary>
    ///     Pairs a chain message with its execution log for tracking execution status.
    /// </summary>
    private record ExecutionItem(ChainMessage Message, ChainExecutionLog ExecutionLog);
}