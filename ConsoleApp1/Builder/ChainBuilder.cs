using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConsoleApp1.Builder;

public sealed class ChainBuilder<TContext>(IServiceCollection services, ChainExecutor<TContext> chainExecutor)
    where TContext : class, ICloneable, new()
{
    public ChainBuilder<TContext> AddChainHandler<TChainHandler>(TChainHandler handler) where TChainHandler : class, IChainHandler<TContext>
    {
        chainExecutor.AddHandler(handler);
        services.TryAddScoped<IChainHandler<TContext>, TChainHandler>();
        return this;
    }

    public IServiceCollection Services => services;
}

public static class ChainBuilderExtensions
{
    public static ChainBuilder<TContext> ComposeChain<TContext>(this IServiceCollection services, string key)
        where TContext : class, ICloneable, new()
    {
        var chainExecutor = new ChainExecutor<TContext>();
        services.TryAddKeyedSingleton(key, chainExecutor);
        return new ChainBuilder<TContext>(services, chainExecutor);
    }
}