namespace Chainer.Results;

public static class ResultConversion
{
    // Convert non-generic Result to Result<T>
    public static Result<T> ToResult<T>(this Result result, T value)
    {
        return result.IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(result.Error);
    }

    public static Result<T> ToResult<T>(this Result result, Func<T> valueFactory)
    {
        return result.IsSuccess ? Result<T>.Success(valueFactory()) : Result<T>.Failure(result.Error);
    }

    public static async Task<Result<T>> ToResultAsync<T>(this Result result, Func<Task<T>> valueFactory)
    {
        return result.IsSuccess ? Result<T>.Success(await valueFactory()) : Result<T>.Failure(result.Error);
    }

    // Convert between Result<TSource> and Result<TTarget>
    public static Result<TTarget> ToResult<TSource, TTarget>(this Result<TSource> source)
        where TSource : TTarget
    {
        return source.IsSuccess ? Result<TTarget>.Success(source.Value) : Result<TTarget>.Failure(source.Error);
    }

    public static Result<TTarget> Convert<TSource, TTarget>(
        this Result<TSource> source,
        Func<TSource, TTarget> converter)
    {
        return source.IsSuccess ? Result<TTarget>.Success(converter(source.Value)) : Result<TTarget>.Failure(source.Error);
    }

    public static async Task<Result<TTarget>> ConvertAsync<TSource, TTarget>(
        this Result<TSource> source,
        Func<TSource, Task<TTarget>> converter)
    {
        return source.IsSuccess ? Result<TTarget>.Success(await converter(source.Value)) : Result<TTarget>.Failure(source.Error);
    }
}