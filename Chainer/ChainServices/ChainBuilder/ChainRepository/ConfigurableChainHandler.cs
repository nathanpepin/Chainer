using Chainer.ChainServices.ChainBuilder.ConfigurationHandlers;
using Microsoft.Extensions.Logging;

namespace Chainer.ChainServices.ChainBuilder.ChainRepository;

public abstract class ConfigurableChainHandler<TContext, TConfig> : IConfigurableChainHandler<TContext>
    where TContext : class, ICloneable, new()
    where TConfig : class, new()
{
    protected TConfig Configuration { get; private set; } = new();

    public void Configure(IHandlerConfiguration configuration)
    {
        Configuration = configuration.Bind<TConfig>();
    }

    public abstract Task<Result<TContext>> Handle(
        TContext context,
        ILogger? logger = null,
        CancellationToken cancellationToken = default);
}