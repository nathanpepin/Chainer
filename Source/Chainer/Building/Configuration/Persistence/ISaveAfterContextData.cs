namespace Chainer.Building.Configuration.Persistence;

/// <summary>
/// Marker interface that indicates a chain handler wants the context data serialized 
/// after execution.
/// </summary>
/// <remarks>
/// When a chain handler implements this interface, the DynamicChainExecutor will automatically 
/// serialize the outgoing context object (from the handler's Result) to JSON and store it in the 
/// ChainExecutionLog's AfterJson property. This allows for:
/// 
/// <list type="bullet">
///   <item>Tracking the state of the context after it was modified by the handler</item>
///   <item>Auditing or debugging the chain execution process</item>
///   <item>Analyzing how each handler affects the context</item>
///   <item>Visualizing the progression of data through the chain</item>
/// </list>
/// 
/// This interface has no methods to implement - it purely acts as a signal to the execution system.
/// 
/// Implement this interface only when context data tracking is necessary, as the serialization 
/// process adds overhead to the execution and increases storage requirements for logs.
/// 
/// Example usage:
/// <code>
/// public class DiscountHandler : IChainHandler&lt;OrderContext&gt;, ISaveAfterContextData
/// {
///     public Task&lt;Result&lt;OrderContext&gt;&gt; Handle(OrderContext context, ILogger? logger, CancellationToken cancellationToken)
///     {
///         context.TotalPrice *= 0.9m; // Apply 10% discount
///         return Task.FromResult&lt;Result&lt;OrderContext&gt;&gt;(context);
///         // The modified context will be automatically serialized after Handle returns
///     }
/// }
/// </code>
/// 
/// Often used in conjunction with ISaveBeforeContextData to capture both the before and after states,
/// enabling complete tracking of the changes made by each handler.
/// </remarks>
public interface ISaveAfterContextData;