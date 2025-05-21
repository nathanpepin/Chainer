using Microsoft.Extensions.Logging;

namespace Chainer.Core;

/// <summary>
///     Builder for creating and configuring a chain of handlers.
/// </summary>
/// <typeparam name="TContext">The context type that will flow through the chain</typeparam>
public sealed class ChainBuilder<TContext> where TContext : class, ICloneable, new()
{
    private readonly List<IChainHandler<TContext>> _handlers = [];
    private bool _cloneContextInHistory = true;
    private ILogger? _logger;

    /// <summary>
    ///     Adds a handler to the chain.
    /// </summary>
    /// <param name="handler">The handler to add</param>
    /// <returns>The chain builder for fluent chaining</returns>
    public ChainBuilder<TContext> AddHandler(IChainHandler<TContext> handler)
    {
        _handlers.Add(handler);
        return this;
    }

    /// <summary>
    ///     Adds multiple handlers to the chain.
    /// </summary>
    /// <param name="handlers">The handlers to add</param>
    /// <returns>The chain builder for fluent chaining</returns>
    public ChainBuilder<TContext> AddHandlers(IEnumerable<IChainHandler<TContext>> handlers)
    {
        _handlers.AddRange(handlers);
        return this;
    }

    /// <summary>
    ///     Sets the logger for the chain.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <returns>The chain builder for fluent chaining</returns>
    public ChainBuilder<TContext> WithLogger(ILogger logger)
    {
        _logger = logger;
        return this;
    }

    /// <summary>
    ///     Enables tracking of execution history.
    /// </summary>
    /// <param name="cloneContext">Whether to clone the context at each step (default: true)</param>
    /// <returns>The chain builder for fluent chaining</returns>
    public ChainBuilder<TContext> TrackExecutionHistory(bool cloneContext = true)
    {
        _cloneContextInHistory = cloneContext;
        return this;
    }

    /// <summary>
    ///     Builds the chain executor.
    /// </summary>
    /// <returns>A ChainExecutor configured with the specified options</returns>
    public ChainExecutor<TContext> Build()
    {
        return new ChainExecutor<TContext>(_handlers, _logger);
    }
}