using Chainer.Core;

namespace Chainer.Building.Configuration;

/// <summary>
///     Allows a chain to be configurable using a given configuration
/// </summary>
/// <typeparam name="TContext"></typeparam>
public interface IConfigurableChainHandler<TContext> : IChainHandler<TContext>
    where TContext : class, ICloneable, new()
{
    void Configure(IHandlerConfiguration configuration);
}