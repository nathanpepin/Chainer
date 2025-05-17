namespace Chainer.ChainServices.ChainBuilder.ConfigurationHandlers;

public interface IConfigurableChainHandler<TContext> : IChainHandler<TContext>
    where TContext : class, ICloneable, new()
{
    void Configure(IHandlerConfiguration configuration);
}