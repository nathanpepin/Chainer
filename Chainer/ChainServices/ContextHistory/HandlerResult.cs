namespace Chainer.ChainServices.ContextHistory;

/// <summary>
///     The result of an handler execution
/// </summary>
/// <param name="Handler">The fullname of the handler that executed</param>
/// <param name="Context">The context that has acted upon</param>
/// <param name="Start">The time the handler started execution</param>
/// <param name="End">The time the handler finished execution</param>
/// <typeparam name="TContext"></typeparam>
public record HandlerResult<TContext>(string Handler, TContext Context, DateTime Start, DateTime End)
    where TContext : class, ICloneable, new()
{
    /// <summary>
    ///     The duration of the handler execution
    /// </summary>
    public TimeSpan Duration => End - Start;
}