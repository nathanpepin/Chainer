using System.Text.Json;
using Chainer.Building.Configuration;
using Chainer.Building.Messages;
using Chainer.Building.Repository;

namespace Chainer.Building.ChainDefinitionService;

public sealed class ChainDefinitionService(IChainRepository repository) : IChainDefinitionService
{
    private readonly IHandlerConfiguration _handlerConfiguration = new ChainHandlerConfiguration(new object(), HandlerConfigurationType.Object);

    public async Task<Result<Guid>> CreateChainAsync<TContext>(
        List<ChainCommand> handlers,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new()
    {
        var chainId = Guid.NewGuid();

        var messages = handlers
            .Select(h => CreateChainMessage<TContext>(chainId, h))
            .ToList();

        var result = await repository.SaveChainMessagesAsync(chainId, messages, cancellationToken);

        return result.IsFailure
            ? Failure<Guid>(result.Error)
            : Success(chainId);
    }

    private ChainMessage CreateChainMessage<TContext>(Guid chainId, ChainCommand h) where TContext : class, ICloneable, new()
    {
        _handlerConfiguration.SetConfiguration(h.Configuration, h.ConfigurationType);
        var jsonValue = JsonSerializer.Serialize(_handlerConfiguration);

        return new ChainMessage
        {
            Id = Guid.NewGuid(),
            ChainId = chainId,
            HandlerTypeName = h.HandlerType.AssemblyQualifiedName ?? h.HandlerType.FullName ?? h.HandlerType.Name,
            ConfigurationJson = jsonValue,
            ExecutionOrder = h.Order,
            ContextTypeName = typeof(TContext).AssemblyQualifiedName ?? typeof(TContext).FullName ?? typeof(TContext).Name
        };
    }
}