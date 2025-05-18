namespace Chainer.ChainServices.ChainBuilder.ChainDefinitionService;

public interface IChainDefinitionService
{
    Task<Result<Guid>> CreateChainAsync<TContext>(
        List<ChainCommand> handlers,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();
}