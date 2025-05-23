using Chainer.Configuration;
using Chainer.Execution;

namespace Chainer.Abstractions;

/// <summary>
///     Defines the contract for executing dynamically configured chains of handlers.
/// </summary>
/// <remarks>
///     This interface provides methods to execute chains of handlers identified by different means:
///     <list type="bullet">
///         <item>The default chain for a context type</item>
///         <item>A chain identified by a friendly name</item>
///         <item>A chain identified by a GUID</item>
///         <item>A chain defined by an in-memory collection of ChainMessage objects</item>
///     </list>
///     It serves as an abstraction over the chain execution system, allowing for:
///     <list type="bullet">
///         <item>Decoupling client code from chain execution details</item>
///         <item>Easier testing with mock implementations</item>
///         <item>Support for multiple execution strategies (in-memory, distributed, etc.)</item>
///         <item>Consistent interface across different execution contexts</item>
///     </list>
///     Implementations of this interface are responsible for:
///     <list type="bullet">
///         <item>Retrieving chain configurations from a repository</item>
///         <item>Instantiating and configuring handlers</item>
///         <item>Executing handlers in the correct order</item>
///         <item>Managing error handling and context passing</item>
///         <item>Recording execution logs and results</item>
///     </list>
/// </remarks>
public interface IDynamicChainExecutor
{
    /// <summary>
    ///     Executes the default chain for the specified context type.
    /// </summary>
    Task<ChainExecutionResult<TContext>> ExecuteDefaultChainAsync<TContext>(
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    /// <summary>
    ///     Executes a chain identified by a friendly name for the specified context type.
    /// </summary>
    Task<ChainExecutionResult<TContext>> ExecuteChainAsync<TContext>(
        string friendlyName,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    /// <summary>
    ///     Executes a chain identified by its GUID for the specified context type.
    /// </summary>
    Task<ChainExecutionResult<TContext>> ExecuteChainAsync<TContext>(
        Guid chainId,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    /// <summary>
    ///     Executes a chain defined by a collection of ChainMessage objects for the specified context type.
    /// </summary>
    Task<ChainExecutionResult<TContext>> ExecuteChainAsync<TContext>(
        IEnumerable<ChainMessage> chainMessages,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();
}