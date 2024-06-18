using System.Text;
using CSharpFunctionalExtensions;

namespace Chainer.ChainServices.ContextHistory;

public sealed class ContextHistoryResult<TContext> where TContext : class, ICloneable, new()

{
    public Result<TContext> Result { get; set; }

    public List<HandlerResult<TContext>> History { get; } = [];
    
    public List<string> Handlers { get; } = [];

    public List<string> UnappliedHandlers { get; } = [];
    
    public DateTime Start { get; init; }
    public DateTime End { get; set; }

    public TimeSpan ExecutionTime => End - Start;

    public override string ToString()
    {
        StringBuilder stringBuilder = new();

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
        foreach (var history in History)
        {
            stringBuilder.AppendLine($"\t-{history.Handler}; Duration: {history.Duration:g}");
        }

        if (UnappliedHandlers.Count != 0)
        {
            stringBuilder.AppendLine("Not Applied Handlers");
            foreach (var handler in UnappliedHandlers)
            {
                stringBuilder.AppendLine($"\t-{handler}");
            }
        }
        
        stringBuilder.AppendLine("----------------------------------------");
        
        return stringBuilder.ToString();
    }
}