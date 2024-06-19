using System.Collections.Immutable;
using Chainer.Calculation;
using Chainer.ChainServices.ContextHistory;

namespace Chainer.ChainServices;

public sealed class ChainExecutor<TContext>(IEnumerable<IChainHandler<TContext>>? handlers = null)
    where TContext : class, ICloneable, new()
{
    private List<IChainHandler<TContext>> ChainHandlers { get; } = handlers?.ToList() ?? [];

    public ChainExecutor<TContext> AddHandler(IChainHandler<TContext> handler)
    {
        ChainHandlers.Add(handler);
        return this;
    }

    public async Task<Result<TContext>> Execute(TContext? context = null, CancellationToken cancellationToken = default)
    {
        context ??= new TContext();

        if (ChainHandlers.Count == 0) return Failure<TContext>("There were no handlers to execute");

        var queue = new Queue<IChainHandler<TContext>>(ChainHandlers);

        while (queue.Count != 0)
        {
            var result = await Try(() => queue
                .Dequeue()
                .Handle(context, cancellationToken));

            var flattenedResult = result.Flatten();
            if (flattenedResult.IsFailure) return flattenedResult;
        }

        return context;
    }

    public async Task<ContextHistoryResult<TContext>> ExecuteWithHistory(TContext context,
        bool doNotCloneContext = false,
        CancellationToken cancellationToken = default)
    {
        var output = new ContextHistoryResult<TContext>
        {
            Start = DateTime.UtcNow
        };

        if (ChainHandlers.Count == 0)
        {
            output.Result = Failure<TContext>("There were no handlers to execute");
            output.End = DateTime.Now;
            return output;
        }

        var handlerNames = ChainHandlers
            .Select(x => x.GetType().FullName ?? "Could not get name")
            .ToImmutableArray();
        output.Handlers.AddRange(handlerNames);

        var queue = new Queue<IChainHandler<TContext>>(ChainHandlers);

        while (queue.Count != 0)
        {
            var handler = queue.Dequeue();

            var start = DateTime.UtcNow;
            var result = await Try(() => handler.Handle(context, cancellationToken));

            var flattenedResult = result.Flatten();
            output.Result = flattenedResult;

            if (flattenedResult.IsFailure)
            {
                output.UnappliedHandlers.Add(handler.GetType().FullName ?? "Could not get name");

                var unappliedHandlerNames = queue
                    .Select(x => x.GetType().FullName ?? "Could not get name")
                    .ToImmutableArray();
                output.UnappliedHandlers.AddRange(unappliedHandlerNames);

                output.End = DateTime.UtcNow;

                return output;
            }

            output.History.Add(new HandlerResult<TContext>(
                handler.GetType().FullName ?? "Could not get name",
                doNotCloneContext ? context : (TContext)context.Clone(),
                start,
                DateTime.UtcNow));
        }

        output.End = DateTime.UtcNow;
        return output;
    }
}