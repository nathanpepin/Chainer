using System.Text.Json;
using CSharpFunctionalExtensions;

namespace ConsoleApp1;

public sealed class ChainExecutor<TContext>(IEnumerable<IChainHandler<TContext>>? handlers = null) 
    where TContext : class, ICloneable, new()
{
    private List<IChainHandler<TContext>> ChainHandlers { get; } = handlers?.ToList() ?? [];

    public ChainExecutor<TContext> AddHandler(IChainHandler<TContext> handler)
    {
        ChainHandlers.Add(handler);
        return this;
    }

    public async Task<Result<TContext>> Execute(TContext context, CancellationToken cancellationToken = default)
    {
        if (ChainHandlers.Count == 0) return Result.Failure<TContext>("There were no handlers to execute");

        var queue = new Queue<IChainHandler<TContext>>(ChainHandlers);

        while (queue.Count != 0)
        {
            var result = await Result.Try(() => queue
                .Dequeue()
                .Handle(context, cancellationToken));

            var flattenedResult = result.Flatten();
            if (flattenedResult.IsFailure) return flattenedResult;
        }

        return context;
    }

    public async Task<ContextHistoryResult<TContext>> ExecuteWithHistory(TContext context,
        CancellationToken cancellationToken = default)
    {
        var output = new ContextHistoryResult<TContext>();
        output.History.Add(new HandlerResult<TContext>("Initial", (TContext)context.Clone(), DateTime.Now, DateTime.Now));

        if (ChainHandlers.Count == 0)
        {
            output.Result = Result.Failure<TContext>("There were no handlers to execute");
            return output;
        }

        var queue = new Queue<IChainHandler<TContext>>(ChainHandlers);
        
        while (queue.Count != 0)
        {
            var handler = queue.Dequeue();

            var start = DateTime.Now;
            var result = await Result.Try(() => handler.Handle(context, cancellationToken));
            
            var flattenedResult = result.Flatten();
            output.Result = flattenedResult;
            
            if (flattenedResult.IsFailure)
                return output;

            var serializedContext = JsonSerializer.Serialize(flattenedResult.Value);
            var clonedContext = JsonSerializer.Deserialize<TContext>(serializedContext) ?? 
                                throw new JsonException($"{typeof(TContext).FullName} must be JSON serializable for cloning purposes");
            
            output.History.Add(new HandlerResult<TContext>(handler.GetType().FullName ?? "Could not get name",
                clonedContext, start, DateTime.Now));
        }

        return output;
    }
}