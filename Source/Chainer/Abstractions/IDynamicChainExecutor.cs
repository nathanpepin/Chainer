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
    /// <typeparam name="TContext">The type of context to process</typeparam>
    /// <param name="initialContext">The initial context to process, or null to create a new instance</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>A result containing the processed context and execution logs</returns>
    /// <remarks>
    ///     The default chain is a predefined chain that is identified by a standard GUID
    ///     (typically InMemoryChainRepository.DefaultChainGuid). It serves as a convenient
    ///     entry point for simple scenarios where a single chain per context type is sufficient.
    ///     If no initial context is provided, a new instance will be created using the
    ///     parameterless constructor of TContext.
    ///     Example usage:
    ///     <code>
    /// var result = await executor.ExecuteDefaultChainAsync&lt;OrderContext&gt;(new OrderContext { OrderId = 123 });
    /// if (result.Context.IsSuccess) 
    /// {
    ///     var processedOrder = result.Context.Value;
    ///     // Use the processed order...
    /// }
    /// </code>
    /// </remarks>
    Task<ChainExecutionResult<TContext>> ExecuteDefaultChainAsync<TContext>(
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    /// <summary>
    ///     Executes a chain identified by a friendly name for the specified context type.
    /// </summary>
    /// <typeparam name="TContext">The type of context to process</typeparam>
    /// <param name="friendlyName">The friendly name of the chain to execute</param>
    /// <param name="initialContext">The initial context to process, or null to create a new instance</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>A result containing the processed context and execution logs</returns>
    /// <remarks>
    ///     This method allows chains to be identified by human-readable names instead of GUIDs.
    ///     The friendly name is typically converted to a deterministic GUID internally to
    ///     ensure consistent identification across systems.
    ///     This approach is useful for:
    ///     <list type="bullet">
    ///         <item>Configuration-driven systems where chains are defined in settings files</item>
    ///         <item>Business-oriented scenarios where chains are named after business processes</item>
    ///         <item>Scenarios where chain identifiers need to be remembered or communicated by humans</item>
    ///     </list>
    ///     Example usage:
    ///     <code>
    /// var result = await executor.ExecuteChainAsync&lt;OrderContext&gt;("OrderProcessingChain", orderContext);
    /// </code>
    /// </remarks>
    Task<ChainExecutionResult<TContext>> ExecuteChainAsync<TContext>(
        string friendlyName,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    /// <summary>
    ///     Executes a chain identified by its GUID for the specified context type.
    /// </summary>
    /// <typeparam name="TContext">The type of context to process</typeparam>
    /// <param name="chainId">The unique identifier of the chain to execute</param>
    /// <param name="initialContext">The initial context to process, or null to create a new instance</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>A result containing the processed context and execution logs</returns>
    /// <remarks>
    ///     This method is the most direct way to execute a chain by its unique identifier.
    ///     It is typically used when:
    ///     <list type="bullet">
    ///         <item>Chain IDs are stored in databases or other structured storage</item>
    ///         <item>Multiple chains need to be distinctly identified programmatically</item>
    ///         <item>Chains are dynamically created and their IDs are generated at runtime</item>
    ///     </list>
    ///     The implementation should retrieve the chain configuration for the specified ID
    ///     from the repository and execute it.
    ///     Example usage:
    ///     <code>
    /// // Either a stored ID or one generated from a friendly name
    /// Guid chainId = GuidFromString.CreateDeterministicGuid("OrderProcessingChain");
    /// var result = await executor.ExecuteChainAsync&lt;OrderContext&gt;(chainId, orderContext);
    /// </code>
    /// </remarks>
    Task<ChainExecutionResult<TContext>> ExecuteChainAsync<TContext>(
        Guid chainId,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();

    /// <summary>
    ///     Executes a chain defined by a collection of ChainMessage objects for the specified context type.
    /// </summary>
    /// <typeparam name="TContext">The type of context to process</typeparam>
    /// <param name="chainMessages">The collection of ChainMessage objects defining the chain</param>
    /// <param name="initialContext">The initial context to process, or null to create a new instance</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>A result containing the processed context and execution logs</returns>
    /// <remarks>
    ///     This method allows for ad-hoc chain execution without requiring the chain to be
    ///     pre-registered in a repository. It is useful for:
    ///     <list type="bullet">
    ///         <item>Dynamically generated chains based on runtime conditions</item>
    ///         <item>One-time execution chains that don't need to be persisted</item>
    ///         <item>Testing and prototyping without repository setup</item>
    ///         <item>Creating chains from custom configuration sources</item>
    ///     </list>
    ///     The chain messages should define the handlers to execute, their execution order,
    ///     and any configuration they need. The implementation is responsible for sorting
    ///     the messages by execution order and executing the handlers accordingly.
    ///     Example usage:
    ///     <code>
    /// var chainMessages = new List&lt;ChainMessage&gt;
    /// {
    ///     new ChainMessage
    ///     {
    ///         Id = Guid.NewGuid(),
    ///         HandlerTypeName = typeof(ValidationHandler).AssemblyQualifiedName,
    ///         ExecutionOrder = 1,
    ///         // Other properties...
    ///     },
    ///     new ChainMessage
    ///     {
    ///         Id = Guid.NewGuid(),
    ///         HandlerTypeName = typeof(ProcessingHandler).AssemblyQualifiedName,
    ///         ExecutionOrder = 2,
    ///         // Other properties...
    ///     }
    /// };
    /// 
    /// var result = await executor.ExecuteChainAsync&lt;OrderContext&gt;(chainMessages, orderContext);
    /// </code>
    /// </remarks>
    Task<ChainExecutionResult<TContext>> ExecuteChainAsync<TContext>(
        IEnumerable<ChainMessage> chainMessages,
        TContext? initialContext = null,
        CancellationToken cancellationToken = default)
        where TContext : class, ICloneable, new();
}