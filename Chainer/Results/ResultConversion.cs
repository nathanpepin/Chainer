namespace Chainer.Results;

public static class ResultConversion
{
    // Conversion between Result<TSource> and Result<TTarget> where TSource can be implicitly converted to TTarget
    public static Result<TTarget> ToResult<TSource, TTarget>(this Result<TSource> source) where TSource : TTarget
    {
        return source.IsSuccess
            ? Result.Success<TTarget>(source.Value)
            : Result.Failure<TTarget>(source.Error);
    }

    // Explicit operator can't be defined as extension method, so this is a utility method for converting
    public static Result<TTarget> Convert<TSource, TTarget>(Result<TSource> source, Func<TSource, TTarget> converter)
    {
        return source.IsSuccess
            ? Result.Success(converter(source.Value))
            : Result.Failure<TTarget>(source.Error);
    }
}