using System.Text;

namespace Chainer.Execution;

/// <summary>
///     Contains the results of a dynamic chain execution, including both the final context state
///     and execution logs for each handler in the chain.
/// </summary>
/// <typeparam name="TContext">The type of context that was processed by the chain</typeparam>
/// <param name="Context">The result containing either the processed context or failure information</param>
/// <param name="ExecutionLogs">An immutable array of execution logs, one for each handler in the chain</param>
public sealed record ChainExecutionResult<TContext>(Result<TContext> Context, ImmutableArray<ChainExecutionLog> ExecutionLogs)
    where TContext : class, ICloneable, new()
{
    public bool IsSuccess => Context.IsSuccess;

    public bool IsFailure => Context.IsFailure;

    public override string ToString()
    {
        return ToString(true);
    }

    /// <summary>
    /// Outputs the execution result to a string
    /// </summary>
    /// <param name="writeArguments">If true, the arguments of each handler will be written to the output string.</param>
    /// <returns></returns>
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
            output.AppendLine($"Error: {Context.Error}");
        else
            output.AppendLine("Error: None");

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
            if (handlerName.Contains("Version=") && handlerName.Contains("Culture=") && handlerName.Contains("PublicKeyToken=")) handlerName = handlerName.Split(',').First();

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