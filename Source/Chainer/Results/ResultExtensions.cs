namespace Chainer.Results;

/// <summary>
/// Provides extension methods for the <see cref="Result{T}"/> and <see cref="Result"/> types, enabling operations such as flattening, combining, and handling success or failure conditions.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Flattens a nested result of type <see cref="Result{Result{T}}"/> into a single <see cref="Result{T}"/>.
    /// If the input result is in a failure state, it propagates the error or exception to the flattened result.
    /// If the input result is successful, it extracts and returns the inner result.
    /// </summary>
    /// <typeparam name="T">The type of the value contained in the result.</typeparam>
    /// <param name="result">The result of type <see cref="Result{Result{T}}"/> to be flattened.</param>
    /// <returns>
    /// A flattened result of type <see cref="Result{T}"/>. If the input result is a failure, it returns a failure result with the
    /// propagated error or exception. Otherwise, it returns the extracted inner result.
    /// </returns>
    public static Result<T> Flatten<T>(this Result<Result<T>> result)
    {
        return result.IsFailure ? result.Exception != null ? Result<T>.Failure(result.Exception) : Result<T>.Failure(result.Error) : result.Value;
    }

    /// <summary>
    /// Executes a given action within a try-catch block, returning a successful result if no exception occurs,
    /// or capturing the exception in a failure result if an exception is thrown.
    /// </summary>
    /// <param name="action">The action to execute. Must not be null.</param>
    /// <returns>A successful result if the action executes without exceptions; otherwise, a failure result containing the captured exception.</returns>
    public static Result Try(Action action)
    {
        try
        {
            action();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Executes an asynchronous operation and encapsulates its result in a <see cref="Result"/> structure.
    /// Captures any exceptions that occur during the execution of the given asynchronous action.
    /// </summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the success or failure of the operation.
    /// If the operation succeeds, the result will be a successful result.
    /// If an exception is thrown, the result will be a failure encapsulating the exception.
    /// </returns>
    public static async Task<Result> TryAsync(Func<Task> action)
    {
        try
        {
            await action();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Executes a provided function and captures its result within a <see cref="Result{T}"/> object.
    /// If the function executes successfully, a successful <see cref="Result{T}"/> is returned containing the result of the function.
    /// If an exception occurs during the execution of the function, a failed <see cref="Result{T}"/> is returned with the exception captured.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="func">The function to be executed, encapsulating the logic that may throw an exception.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> object representing the outcome of the function execution.
    /// If successful, it contains the resulting value of type <typeparamref name="T"/>. 
    /// In case of failure, it includes the associated exception.
    /// </returns>
    public static Result<T> Try<T>(Func<T> func)
    {
        try
        {
            return Result<T>.Success(func());
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex);
        }
    }

    /// <summary>
    /// Attempts to asynchronously execute the provided function and captures its result.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the function.</typeparam>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> instance representing the success or failure of the function execution.
    /// On success, contains the result of the function. On failure, contains the captured exception.
    /// </returns>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> func)
    {
        try
        {
            return Result<T>.Success(await func());
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex);
        }
    }

    // Convenience methods for making code more readable
    
    /// <summary>
    /// Returns a successful result.
    /// </summary>
    /// <returns>Returns an instance of a successful <see cref="Result"/>.</returns>
    public static Result Success()
    {
        return Result.Success();
    }

    /// <summary>
    /// Returns a successful result containing the provided value.
    /// </summary>
    /// <typeparam name="T">The type of the value to encapsulate.</typeparam>
    /// <param name="value">The value to encapsulate in the successful result.</param>
    /// <returns>A <see cref="Result{T}"/> instance containing the provided value and indicating success.</returns>
    public static Result<T> Success<T>(T value)
    {
        return Result<T>.Success(value);
    }

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message associated with the failure result.</param>
    /// <returns>A failure <see cref="Result"/> containing the specified error message.</returns>
    public static Result Failure(string error)
    {
        return Result.Failure(error);
    }

    /// <summary>
    /// Creates a failure <see cref="Result{T}"/> instance with the specified error message.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>A failure result containing the provided error message.</returns>
    public static Result<T> Failure<T>(string error)
    {
        return Result<T>.Failure(error);
    }

    /// <summary>
    /// Creates a failure result with the specified exception.
    /// </summary>
    /// <param name="exception">The exception associated with the failure.</param>
    /// <returns>A failure <see cref="Result"/> containing the provided exception.</returns>
    public static Result Failure(Exception exception)
    {
        return Result.Failure(exception);
    }

    /// <summary>
    /// Creates a failure <see cref="Result{T}"/> with the specified exception.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="exception">The exception to associate with the failure result.</param>
    /// <returns>A failure result containing the specified exception.</returns>
    public static Result<T> Failure<T>(Exception exception)
    {
        return Result<T>.Failure(exception);
    }

    /// <summary>
    /// Combines a collection of results into a single result based on the provided combiner function.
    /// If all results are successful, their values are combined using the combiner function, and a successful result is returned.
    /// If one or more results are failures, a failure result is returned with either the first encountered exception or a combined error message.
    /// </summary>
    /// <typeparam name="T">The type of the result values.</typeparam>
    /// <param name="results">The collection of results to combine.</param>
    /// <param name="combiner">The function to combine the values of successful results into a single value.</param>
    /// <returns>
    /// A combined result. If all results are successful, a successful result containing the combined value is returned. 
    /// If there are any failures, a failure result is returned with either the first encountered exception or a combined error message.
    /// </returns>
    public static Result<T> Combine<T>(this IEnumerable<Result<T>> results, Func<IEnumerable<T>, T> combiner)
    {
        var resultsList = results.ToList();
        var failures = resultsList.Where(x => x.IsFailure).ToList();

        if (failures.Count == 0) return Result<T>.Success(combiner(resultsList.Select(x => x.Value)));
        {
            // Try to find the first exception to preserve
            var firstWithException = failures.FirstOrDefault(f => f.Exception != null);
            return firstWithException.IsFailure
                ? Result<T>.Failure(firstWithException.Exception!)
                :
                // Otherwise return combined error messages
                Result<T>.Failure(string.Join(", ", failures.Select(x => x.Error)));
        }
    }

    /// <summary>
    /// Combines multiple <see cref="Result"/> objects into a single <see cref="Result"/>. 
    /// If all results are successful, the combined result will also be successful. 
    /// If any result has failed, the combined result will be a failure, propagating the first
    /// exception found or concatenating error messages from all failed results.
    /// </summary>
    /// <param name="results">A collection of <see cref="Result"/> objects to combine.</param>
    /// <returns>
    /// A combined <see cref="Result"/>. If all input results are successful, returns a successful result. 
    /// If any input result has failed, returns a failure result containing combined error messages or the first exception found.
    /// </returns>
    public static Result Combine(this IEnumerable<Result> results)
    {
        var resultsList = results.ToList();
        var failures = resultsList.Where(x => x.IsFailure).ToList();

        if (failures.Count == 0) return Result.Success();
        {
            // Try to find the first exception to preserve
            var firstWithException = failures.FirstOrDefault(f => f.Exception != null);
            return firstWithException.IsFailure
                ? Result.Failure(firstWithException.Exception!)
                :
                // Otherwise return combined error messages
                Result.Failure(string.Join(", ", failures.Select(x => x.Error)));
        }
    }
}