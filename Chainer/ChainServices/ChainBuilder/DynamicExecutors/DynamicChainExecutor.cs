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
    public async Task<Result<TContext>> ExecuteChainAsync<TContext>(
        Guid chainId,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new()
    {
        // Get chain messages
        var messagesResult = await repository.GetChainMessagesAsync(chainId, cancellationToken);
        if (messagesResult.IsFailure)
            return Failure<TContext>(messagesResult.Error);

        var messages = messagesResult.Value
            .OrderBy(m => m.ExecutionOrder)
            .ToList();

        // Initialize context if not provided
        initialContext ??= new TContext();
        var context = initialContext;

        // Execute each handler in order
        foreach (var message in messages)
            try
            {
                // Update status to executing
                await repository.UpdateChainMessageStatusAsync(
                    message.Id, ChainMessageStatus.Executing, cancellationToken: cancellationToken);

                // Get handler type
                var handlerType = Type.GetType(message.HandlerTypeName);
                if (handlerType == null)
                    return Failure<TContext>($"Handler type {message.HandlerTypeName} not found");

                // Get handler instance from DI or create new
                var handler = serviceProvider.GetService(handlerType) ??
                              ActivatorUtilities.CreateInstance(serviceProvider, handlerType);

                if (handler is not IChainHandler<TContext> typedHandler)
                    return Failure<TContext>($"Handler {handlerType.Name} does not implement IChainHandler<{typeof(TContext).Name}>");

                // Configure handler if it supports configuration
                if (handler is IConfigurableChainHandler<TContext> configurableHandler &&
                    !string.IsNullOrEmpty(message.ConfigurationJson))
                {
                    var configuration = HandlerConfiguration.FromJson(message.ConfigurationJson);
                    configurableHandler.Configure(configuration);
                }

                // Execute handler
                var result = await typedHandler.Handle(context, logger, cancellationToken);

                if (result.IsFailure)
                {
                    await repository.UpdateChainMessageStatusAsync(
                        message.Id, ChainMessageStatus.Failed, result.Error, cancellationToken);
                    return Failure<TContext>(result.Error);
                }

                // Update context for next handler
                context = result.Value;

                // Update status to completed
                await repository.UpdateChainMessageStatusAsync(
                    message.Id, ChainMessageStatus.Completed, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await repository.UpdateChainMessageStatusAsync(
                    message.Id, ChainMessageStatus.Failed, ex.Message, cancellationToken);
                return Failure<TContext>(ex.Message);
            }

        return Success(context);
    }
}