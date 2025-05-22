using System.Text;

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
public sealed record ChainExecutionResult<TContext>(Result<TContext> Context, ImmutableArray<ChainExecutionLog> ExecutionLogs)
    where TContext : class, ICloneable, new()
{
    public override string ToString()
    {
        return ToString(true);
    }

    public string ToString(bool writeArguments)
    {
        StringBuilder output = new();

        // Header with divider
        output.AppendLine("----------------------------------------");

        // Context type information
        output.AppendLine($"Context: {typeof(TContext).FullName}");

        // Success/failure status and error if applicable
        output.AppendLine($"Success: {Context.IsSuccess}");
        if (Context.IsFailure)
        {
            output.AppendLine($"Error: {Context.Error}");
        }
        else
        {
            output.AppendLine("Error: None");
        }

        // Execution timing information
        var logsWithStartTime = ExecutionLogs.Where(log => log.ExecutedAt.HasValue).ToList();
        var logsWithEndTime = ExecutionLogs.Where(log => log.FinishedAt.HasValue).ToList();

        if (logsWithStartTime.Count != 0 && logsWithEndTime.Count != 0)
        {
            var firstHandlerStart = logsWithStartTime.Min(log => log.ExecutedAt!.Value);
            var lastHandlerFinish = logsWithEndTime.Max(log => log.FinishedAt!.Value);

            output.AppendLine($"Start: {firstHandlerStart:yyyy-MM-ddTHH:mm:ss}");
            output.AppendLine($"End: {lastHandlerFinish:yyyy-MM-ddTHH:mm:ss}");

            var executionTime = lastHandlerFinish - firstHandlerStart;
            output.AppendLine($"Execution Time: {executionTime}");
        }

        // Handler execution summary
        output.AppendLine("Applied Handlers");

        foreach (var log in ExecutionLogs.OrderBy(l => l.ExecutionOrder))
        {
            var handlerName = log.HandlerTypeName;
            // Extract just the class name for readability
            if (handlerName.Contains("Version=") && handlerName.Contains("Culture=") && handlerName.Contains("PublicKeyToken="))
            {
                handlerName = handlerName.Split(',').First();
            }

            var duration = log is { FinishedAt: not null, ExecutedAt: not null }
                ? (log.FinishedAt.Value - log.ExecutedAt.Value).ToString()
                : "N/A";

            output.AppendLine($"\t-{log.Status}: {handlerName}; Duration: {duration};");

            if (!writeArguments || log.ConfigurationJson is null or "{}") continue;

            output.AppendLine("\tArguments: ");

            foreach (var line in log.ConfigurationJson.Split(Environment.NewLine))
            {
                output.Append("\t\t");
                output.AppendLine(line);
            }
        }

        // Footer with divider
        output.AppendLine("----------------------------------------");

        return output.ToString();
    }
}