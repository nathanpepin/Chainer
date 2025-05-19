using Microsoft.Extensions.Logging;

namespace Chainer.Building.Configuration;

public abstract class ConfigurableHandler<TContext, TConfig> : IConfigurableChainHandler<TContext>
    where TContext : class, ICloneable, new()
    where TConfig : class, new()
{
    protected TConfig? Configuration { get; private set; } = new();

    public virtual void Configure(IHandlerConfiguration configuration)
    {
        Configuration = configuration.Bind<TConfig>();
    }

    public abstract Task<Result<TContext>> Handle(
        TContext context,
        ILogger? logger = null,
        CancellationToken cancellationToken = default);
}