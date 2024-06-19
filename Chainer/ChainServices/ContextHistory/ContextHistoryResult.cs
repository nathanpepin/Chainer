using System.Text;

namespace Chainer.ChainServices.ContextHistory;

/// <summary>
///     Contains the result of the chain execution and metadata about the execution
/// </summary>
/// <typeparam name="TContext"></typeparam>
public sealed class ContextHistoryResult<TContext> where TContext : class, ICloneable, new()

{
    /// <summary>
    ///     The result of the chain execution
    /// </summary>
    public Result<TContext> Result { get; set; }

    /// <summary>
    ///     The history of the execution, including start and end times and the state of the context at each step (if enabled)
    /// </summary>
    public List<HandlerResult<TContext>> History { get; } = [];

    /// <summary>
    ///     The handlers that were registered for the execution
    /// </summary>
    public List<string> Handlers { get; } = [];

    /// <summary>
    ///     The handlers that were not applied during the execution
    /// </summary>
    public List<string> UnappliedHandlers { get; } = [];

    /// <summary>
    ///     The starting time of the execution
    /// </summary>
    public DateTime Start { get; init; }

    /// <summary>
    ///     The ending time of the execution
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    ///     The total time taken to execute the chain
    /// </summary>
    public TimeSpan ExecutionTime => End - Start;

    /// <summary>
    ///     Pretty prints the result of the execution
    /// </summary>
    /// <returns></returns>
    public string PrintOutput(bool includeLineBreaks = true)
    {
        StringBuilder stringBuilder = new();

        if (includeLineBreaks)
            stringBuilder.AppendLine("----------------------------------------");

        stringBuilder.AppendLine($"Context: {typeof(TContext).FullName}");
        stringBuilder.AppendLine($"Success: {Result.IsSuccess}");

        if (Result.IsFailure)
            stringBuilder.AppendLine($"Error: {Result.Error}");
        else
            stringBuilder.AppendLine("Error: None");

        stringBuilder.AppendLine($"Start: {Start:s}");
        stringBuilder.AppendLine($"End: {End:s}");
        stringBuilder.AppendLine($"Execution Time: {ExecutionTime:g}");

        stringBuilder.AppendLine("Applied Handlers");
        foreach (var history in History) stringBuilder.AppendLine($"\t-{history.Handler}; Duration: {history.Duration:g}");

        if (UnappliedHandlers.Count != 0)
        {
            stringBuilder.AppendLine("Not Applied Handlers");
            foreach (var handler in UnappliedHandlers) stringBuilder.AppendLine($"\t-{handler}");
        }

        if (includeLineBreaks)
            stringBuilder.AppendLine("----------------------------------------");

        return stringBuilder.ToString();
    }

    public override string ToString()
    {
        return PrintOutput();
    }
}