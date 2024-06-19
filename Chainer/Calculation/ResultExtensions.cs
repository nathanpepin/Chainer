namespace Chainer.Calculation;

internal static class ResultExtensions
{
    public static Result<T> Flatten<T>(this Result<Result<T>> it)
    {
        return it.IsFailure ? Failure<T>(it.Error) : it.Value;
    }
}