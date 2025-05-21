namespace Chainer.Building.Configuration.Persistence;

/// <summary>
/// Marker interface that indicates a chain handler wants the context data serialized 
/// before execution.
/// </summary>
/// <remarks>
/// When a chain handler implements this interface, the DynamicChainExecutor will automatically 
/// serialize the incoming context object to JSON and store it in the ChainExecutionLog's BeforeJson 
/// property. This allows for:
/// 
/// <list type="bullet">
///   <item>Tracking the state of the context before it was modified by the handler</item>
///   <item>Auditing or debugging the chain execution process</item>
///   <item>Analyzing how each handler affects the context</item>
///   <item>Reconstructing the chain's execution path for troubleshooting</item>
/// </list>
/// 
/// This interface has no methods to implement - it purely acts as a signal to the execution system.
/// 
/// Implement this interface only when context data tracking is necessary, as the serialization 
/// process adds overhead to the execution and increases storage requirements for logs.
/// 
/// Example usage:
/// <code>
/// public class PricingHandler : IChainHandler&lt;OrderContext&gt;, ISaveBeforeContextData
/// {
///     // The context will be automatically serialized before Handle is called
///     public Task&lt;Result&lt;OrderContext&gt;&gt; Handle(OrderContext context, ILogger? logger, CancellationToken cancellationToken)
///     {
///         // Handle implementation
///     }
/// }
/// </code>
/// 
/// Often used in conjunction with ISaveAfterContextData to capture both the before and after states.
/// </remarks>
public interface ISaveBeforeContextData;