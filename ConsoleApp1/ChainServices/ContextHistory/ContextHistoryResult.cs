using CSharpFunctionalExtensions;

namespace ConsoleApp1;

public sealed class ContextHistoryResult<TContext> where TContext : class, ICloneable, new()

{
    public Result<TContext> Result { get; set; }

    public List<HandlerResult<TContext>> History { get; } = [];
}

public sealed class ContextHistoryResult<TContext, TOut> where TContext : class, ICloneable, new()

{
    public Result<TOut> Result { get; set; }

    public List<HandlerResult<TContext>> History { get; } = [];
}