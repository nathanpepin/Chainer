namespace ConsoleApp1;

public record HandlerResult<TContext>(string Handler, TContext Context, DateTime Start, DateTime End)
    where TContext : class, ICloneable, new()
{
    public TimeSpan Duration => End - Start;
}