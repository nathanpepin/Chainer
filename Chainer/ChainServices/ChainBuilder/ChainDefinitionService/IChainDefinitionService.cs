using Chainer.ChainServices.ChainBuilder.ConfigurationHandlers;

namespace Chainer.ChainServices.ChainBuilder.ChainDefinitionService;

public interface IChainDefinitionService
{
    Task<Result<Guid>> CreateChainAsync<TContext>(
        List<(Type HandlerType, object Configuration, int Order)> handlers,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    Task<Result<Guid>> CreateChainAsync<TContext>(
        List<(Type HandlerType, string? ConfigurationJson, int Order)> handlers,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    Task<Result<Guid>> CreateChainAsync<TContext>(
        List<(Type HandlerType, IDictionary<string, object?>? Configuration, int Order)> handlers,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    Task<Result<Guid>> CreateChainAsync<TContext>(
        List<(Type HandlerType, IHandlerConfiguration? Configuration, int Order)> handlers,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();
}