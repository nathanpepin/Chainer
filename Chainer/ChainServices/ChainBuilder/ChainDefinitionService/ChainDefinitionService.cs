using System.Text.Json;
using Chainer.ChainServices.ChainBuilder.ChainRepository;
using Chainer.ChainServices.ChainBuilder.ConfigurationHandlers;
using Chainer.ChainServices.ChainBuilder.Messages;

namespace Chainer.ChainServices.ChainBuilder.ChainDefinitionService;

public sealed class ChainDefinitionService(IChainRepository repository) : IChainDefinitionService
{
    // Create chain from JSON configuration
    public Task<Result<Guid>> CreateChainAsync<TContext>(List<(Type HandlerType, object Configuration, int Order)> handlers, CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new()
    {
        throw new NotImplementedException();
    }

    public async Task<Result<Guid>> CreateChainAsync<TContext>(
        List<(Type HandlerType, string? ConfigurationJson, int Order)> handlers,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new()
    {
        var chainId = Guid.NewGuid();
        var messages = handlers.Select(h => new ChainMessageRecord
        {
            Id = Guid.NewGuid(),
            ChainId = chainId,
            HandlerTypeName = h.HandlerType.AssemblyQualifiedName ?? h.HandlerType.FullName ?? h.HandlerType.Name,
            ConfigurationJson = h.ConfigurationJson,
            ExecutionOrder = h.Order,
            ContextTypeName = typeof(TContext).AssemblyQualifiedName ?? typeof(TContext).FullName ?? typeof(TContext).Name
        }).ToList();

        var result = await repository.SaveChainMessagesAsync(chainId, messages, cancellationToken);
        if (result.IsFailure)
            return Failure<Guid>(result.Error);

        return Success(chainId);
    }

    // Create chain from dictionary configuration
    public async Task<Result<Guid>> CreateChainAsync<TContext>(
        List<(Type HandlerType, IDictionary<string, object?>? Configuration, int Order)> handlers,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new()
    {
        var chainId = Guid.NewGuid();
        var messages = handlers.Select(h => new ChainMessageRecord
        {
            Id = Guid.NewGuid(),
            ChainId = chainId,
            HandlerTypeName = h.HandlerType.AssemblyQualifiedName ?? h.HandlerType.FullName ?? h.HandlerType.Name,
            ConfigurationJson = h.Configuration != null
                ? JsonSerializer.Serialize(h.Configuration)
                : null,
            ExecutionOrder = h.Order,
            ContextTypeName = typeof(TContext).AssemblyQualifiedName ?? typeof(TContext).FullName ?? typeof(TContext).Name
        }).ToList();

        var result = await repository.SaveChainMessagesAsync(chainId, messages, cancellationToken);
        if (result.IsFailure)
            return Failure<Guid>(result.Error);

        return Success(chainId);
    }

    // Create chain from IHandlerConfiguration
    public async Task<Result<Guid>> CreateChainAsync<TContext>(
        List<(Type HandlerType, IHandlerConfiguration? Configuration, int Order)> handlers,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new()
    {
        // Implementation would convert the IHandlerConfiguration to JSON for storage
        // This method would be useful for programmatically building complex configurations
        throw new NotImplementedException();
    }
}