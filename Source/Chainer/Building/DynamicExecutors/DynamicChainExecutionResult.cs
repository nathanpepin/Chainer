namespace Chainer.Building.DynamicExecutors;

/// <summary>
///     Contains the results of a dynamic chain execution, including both the final context state
///     and execution logs for each handler in the chain.
/// </summary>
/// <typeparam name="TContext">The type of context that was processed by the chain</typeparam>
/// <param name="Context">The result containing either the processed context or failure information</param>
/// <param name="ExecutionLogs">An immutable array of execution logs, one for each handler in the chain</param>
/// <remarks>
///     This record serves as the unified return type for all DynamicChainExecutor execution methods.
///     It provides both the outcome of the chain execution (success or failure with the final context)
///     and detailed logs about what happened during each step of the execution.
///     The ExecutionLogs array preserves the execution order, allowing for:
///     <list type="bullet">
///         <item>Reconstructing the chain's execution path</item>
///         <item>Analyzing which handlers succeeded, failed, or were skipped</item>
///         <item>Viewing context state before and after each handler (if tracking was enabled)</item>
///         <item>Monitoring execution times and performance metrics</item>
///         <item>Auditing and troubleshooting the chain execution process</item>
///     </list>
///     Being an immutable record, the result cannot be modified after creation, ensuring data integrity.
///     Example usage:
///     <code>
/// // Execute a chain and analyze the results
/// var result = await dynamicExecutor.ExecuteChainAsync&lt;OrderContext&gt;("ProcessOrder", orderContext);
/// 
/// if (result.Context.IsSuccess)
/// {
///     // Use the successfully processed context
///     var processedOrder = result.Context.Value;
///     // ...
/// }
/// else
/// {
///     // Handle failure
///     string errorMessage = result.Context.Error;
///     
///     // Find where in the chain the failure occurred
///     var failedHandler = result.ExecutionLogs.FirstOrDefault(log => log.Status == ChainMessageStatus.Failed);
///     if (failedHandler != null)
///     {
///         Console.WriteLine($"Failed at: {failedHandler.HandlerTypeName}");
///     }
/// }
/// 
/// // Generate execution report
/// foreach (var log in result.ExecutionLogs)
/// {
///     Console.WriteLine($"{log.HandlerTypeName}: {log.Status}");
///     if (log.BeforeJson != null && log.AfterJson != null)
///     {
///         // Compare before/after state
///     }
/// }
/// </code>
/// </remarks>
public sealed record DynamicChainExecutionResult<TContext>(Result<TContext> Context, ImmutableArray<ChainExecutionLog> ExecutionLogs)
    where TContext : class, ICloneable, new();