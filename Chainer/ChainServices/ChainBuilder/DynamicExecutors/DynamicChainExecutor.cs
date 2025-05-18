using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using Chainer.ChainServices.ChainBuilder.ChainRepository;
using Chainer.ChainServices.ChainBuilder.ConfigurationHandlers;
using Chainer.ChainServices.ChainBuilder.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chainer.ChainServices.ChainBuilder.DynamicExecutors;

public sealed class DynamicChainExecutor(
    IChainRepository repository,
    IServiceProvider serviceProvider,
    ILogger<DynamicChainExecutor> logger) : IDynamicChainExecutor
{
    private static readonly ConcurrentDictionary<string, Type?> TypeCache = new();

    public async Task<Result<TContext>> ExecuteChainAsync<TContext>(
        Guid chainId,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new()
    {
        // Get chain messages
        var messagesResult = await repository.GetChainMessagesAsync(chainId, cancellationToken);
        if (messagesResult.IsFailure) return Failure<TContext>(messagesResult.Error);

        return await ExecuteChainAsync(messagesResult.Value, initialContext, cancellationToken);
    }

    public async Task<Result<TContext>> ExecuteChainAsync<TContext>(
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

        return success
            ? Success(resultContext)
            : Failure<TContext>(errorMessage);
    }

    private static ImmutableArray<ExecutionItem> PrepareOrderedExecutionItems(IEnumerable<ChainMessage> chainMessages)
    {
        return
        [
            ..chainMessages
                .OrderBy(m => m.ExecutionOrder)
                .Select(x => new ExecutionItem(x, new ChainExecutionLog(x)))
        ];
    }

    private async Task<(bool success, TContext context, string errorMessage)> ExecuteChainMessagesInOrderAsync<TContext>(
        ImmutableArray<ExecutionItem> executionItems,
        TContext context,
        CancellationToken cancellationToken)
        where TContext : class, ICloneable, new()
    {
        var errorState = false;
        var errorMessage = string.Empty;

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
            await repository.UpdateChainExecutionLog(log, ChainMessageStatus.Executing, cancellationToken);

            var handlerResult = await GetAndExecuteHandlerAsync(message, context, log, cancellationToken);

            if (handlerResult.IsFailure)
            {
                await repository.UpdateChainExecutionLog(log, ChainMessageStatus.Failed, cancellationToken);
                return handlerResult;
            }

            await repository.UpdateChainExecutionLog(log, ChainMessageStatus.Completed, cancellationToken);
            return handlerResult;
        }
        catch (Exception exception)
        {
            await repository.UpdateChainExecutionLog(log, ChainMessageStatus.Error, cancellationToken);
            return Failure<TContext>(exception.Message);
        }
    }

    private async Task<Result<TContext>> GetAndExecuteHandlerAsync<TContext>(
        ChainMessage message,
        TContext context,
        ChainExecutionLog log,
        CancellationToken cancellationToken)
        where TContext : class, ICloneable, new()
    {
        // Get handler type
        var handlerType = TypeCache.GetOrAdd(message.HandlerTypeName, Type.GetType);
        if (handlerType is null)
        {
            return Failure<TContext>($"Handler type {message.HandlerTypeName} not found");
        }

        // Get handler instance from DI or create new
        var handler = serviceProvider.GetService(handlerType) ??
                      ActivatorUtilities.CreateInstance(serviceProvider, handlerType);

        if (handler is not IChainHandler<TContext> typedHandler)
        {
            return Failure<TContext>($"Handler {handlerType.Name} does not implement IChainHandler<{typeof(TContext).Name}>");
        }

        await SaveBeforeContextDataIfNeeded(handler, context, log);
        ConfigureHandlerIfNeeded<TContext>(handler, message);

        var result = await typedHandler.Handle(context, logger, cancellationToken);

        SaveAfterContextDataIfNeeded(log, result, handler);

        return result;
    }

    private static void SaveAfterContextDataIfNeeded<TContext>(ChainExecutionLog log, Result<TContext> result, object handler) where TContext : class, ICloneable, new()
    {
        if (result.IsSuccess && handler is ISaveAfterContextData)
        {
            log.AfterJson = JsonSerializer.Serialize(result.Value);
        }
    }

    private static Task SaveBeforeContextDataIfNeeded<TContext>(object handler, TContext context, ChainExecutionLog log)
        where TContext : class, ICloneable, new()
    {
        if (handler is ISaveBeforeContextData)
        {
            log.BeforeJson = JsonSerializer.Serialize(context);
        }

        return Task.CompletedTask;
    }

    private static void ConfigureHandlerIfNeeded<TContext>(
        object handler,
        ChainMessage message)
        where TContext : class, ICloneable, new()
    {
        if (handler is not IConfigurableChainHandler<TContext> configurableHandler || string.IsNullOrEmpty(message.ConfigurationJson))
        {
            return;
        }

        var configuration = HandlerConfiguration.FromJson(message.ConfigurationJson);
        configurableHandler.Configure(configuration);
    }

    private Task<Result> MarkMessageSkipped(ChainExecutionLog log, CancellationToken cancellationToken)
    {
        return repository.UpdateChainExecutionLog(log, ChainMessageStatus.Skipped, cancellationToken);
    }

    private record ExecutionItem(ChainMessage Message, ChainExecutionLog ExecutionLog);
}