namespace Chainer.Results;

/// Provides extension methods to convert between different `Result` types or to construct a `Result<T>` from a non-generic `Result`.
public static class ResultConversion
{
    // Convert non-generic Result to Result<T>
    /// Converts a non-generic <see cref="Result"/> into a generic <see cref="Result{T}"/> using the specified value.
    /// <param name="result">The source <see cref="Result"/> to be converted.</param>
    /// <param name="value">The value to use for the generic <see cref="Result{T}"/>.</param>
    /// <typeparam name="T">The type of the value to be encapsulated in the result.</typeparam>
    /// <returns>
    /// A <see cref="Result{T}"/> representing success with the specified value if the source result is successful;
    /// otherwise, a failure <see cref="Result{T}"/> containing the error from the source result.
    /// </returns>
    public static Result<T> ToResult<T>(this Result result, T value)
    {
        return result.IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(result.Error);
    }

    /// <summary>
    /// Converts a non-generic <see cref="Result"/> to a generic <see cref="Result{T}"/> using the provided value factory.
    /// </summary>
    /// <typeparam name="T">The type of the value contained in the returned <see cref="Result{T}"/>.</typeparam>
    /// <param name="result">The original non-generic <see cref="Result"/> object to be converted.</param>
    /// <param name="valueFactory">A function that produces the value for the resulting <see cref="Result{T}"/> when the original result is successful.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> if the original result was successful, with the value produced by <paramref name="valueFactory"/>.
    /// If the original result was not successful, a failed <see cref="Result{T}"/> with the same error as the original result.
    /// </returns>
    public static Result<T> ToResult<T>(this Result result, Func<T> valueFactory)
    {
        return result.IsSuccess ? Result<T>.Success(valueFactory()) : Result<T>.Failure(result.Error);
    }

    /// Converts a non-generic Result into a generic Result asynchronously.
    /// <param name="result">The non-generic Result instance to convert.</param>
    /// <param name="valueFactory">
    /// A function that asynchronously provides the value to be included in the resulting generic Result.
    /// This function is only invoked if the original Result is a success.
    /// </param>
    /// <typeparam name="T">The type of the value to be wrapped in the resulting generic Result.</typeparam>
    /// <returns>
    /// A Task containing the generic Result. If the original Result is a success, the returned Result
    /// will also be a success, wrapping the value produced by the valueFactory. If the original Result
    /// is a failure, the returned Result will also be a failure, carrying forward the same error message.
    /// </returns>
    public static async Task<Result<T>> ToResultAsync<T>(this Result result, Func<Task<T>> valueFactory)
    {
        return result.IsSuccess ? Result<T>.Success(await valueFactory()) : Result<T>.Failure(result.Error);
    }

    /// Converts a `Result` instance to a `Result<T>` instance with a provided value.
    /// <param name="result">
    /// The original `Result` instance that indicates success or failure.
    /// </param>
    /// <param name="value">
    /// The value of type `T` that will be assigned to the new `Result<T>` instance.
    /// </param>
    /// <typeparam name="T">
    /// The type of the value to associate with the result.
    /// </typeparam>
    /// <returns>
    /// A `Result<T>` instance that represents either a successful result with the provided value
    /// or a failed result with the same error as the original `Result`.
    /// </returns>
    public static Result<TTarget> ToResult<TSource, TTarget>(this Result<TSource> source)
        where TSource : TTarget
    {
        return source.IsSuccess ? Result<TTarget>.Success(source.Value) : Result<TTarget>.Failure(source.Error);
    }

    /// Converts a `Result<TSource>` to a `Result<TTarget>` using the provided converter function.
    /// <param name="source">
    /// The source result to be converted.
    /// </param>
    /// <param name="converter">
    /// A function that defines how to convert the value of type `TSource` to `TTarget`.
    /// This function is called only if the source result is successful.
    /// </param>
    /// <typeparam name="TSource">
    /// The type of the value in the source result.
    /// </typeparam>
    /// <typeparam name="TTarget">
    /// The type of the value in the resulting result.
    /// </typeparam>
    /// <return>
    /// Returns a `Result<TTarget>`. If the source result is successful, the returned result contains the transformed value.
    /// If the source result is a failure, the returned result contains the same error and exception as the source.
    /// </return>
    public static Result<TTarget> Convert<TSource, TTarget>(
        this Result<TSource> source,
        Func<TSource, TTarget> converter)
    {
        return source.IsSuccess ? Result<TTarget>.Success(converter(source.Value)) : Result<TTarget>.Failure(source.Error);
    }

    /// Converts the source result to a new result with a different value type asynchronously using the specified converter function.
    /// <param name="source">The source result to convert.</param>
    /// <param name="converter">A function that asynchronously converts the value of the source result to the target type.</param>
    /// <typeparam name="TSource">The type of the source value in the result.</typeparam>
    /// <typeparam name="TTarget">The type of the target value in the resulting result.</typeparam>
    /// <returns>An asynchronous task that resolves to a new result containing the converted value if the source is successful, or the original error if it is a failure.</returns>
    public static async Task<Result<TTarget>> ConvertAsync<TSource, TTarget>(
        this Result<TSource> source,
        Func<TSource, Task<TTarget>> converter)
    {
        return source.IsSuccess ? Result<TTarget>.Success(await converter(source.Value)) : Result<TTarget>.Failure(source.Error);
    }
}