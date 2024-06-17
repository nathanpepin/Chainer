using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.ValueTasks;
using Dumpify;

namespace ConsoleApp1;

public sealed class ChainInOutExecutor<TContext, TIn, TOut>(
    Func<TIn, Task<TContext>> import,
    Func<TContext, Task<TOut>> export,
    IEnumerable<IChainHandler<TContext>>? handlers = null) 
    where TContext : class, ICloneable, new()
{
    private List<IChainHandler<TContext>> ChainHandlers { get; } = handlers?.ToList() ?? [];

    public ChainInOutExecutor<TContext, TIn, TOut> AddHandler(IChainHandler<TContext> handler)
    {
        ChainHandlers.Add(handler);
        return this;
    }

    public async Task<Result<TOut>> Execute(TIn input, CancellationToken cancellationToken = default)
    {
        return await Result
            .Try(() => import(input))
            .BindTry(x => new ChainExecutor<TContext>(ChainHandlers).Execute(x, cancellationToken))
            .MapTry(export);
    }

    public async Task<ContextHistoryResult<TContext, TOut>> ExecuteWithHistory(TIn input,
        CancellationToken cancellationToken = default)
    {
        var output = new ContextHistoryResult<TContext, TOut>();

        var (_, importFailure, context, importError) = await Result.Try(() => import(input));

        if (importFailure)
        {
            output.Result = Result.Failure<TOut>(importError);
            return output;
        }

        var chainResult = await new ChainExecutor<TContext>(ChainHandlers)
            .ExecuteWithHistory(context, cancellationToken);

        output.History.AddRange(chainResult.History);

        var (_, chainFailure, chainContext, chainError) = chainResult.Result;
        if (chainFailure)
        {
            output.Result = Result.Failure<TOut>(chainError);
            return output;
        }
        
        output.Result =  await Result.Try(() => export(chainContext));
        return output;
    }
}