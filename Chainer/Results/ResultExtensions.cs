namespace Chainer.Results;

public static class ResultExtensions
{
    // Flatten nested results
    public static Result<T> Flatten<T>(this Result<Result<T>> result) =>
        result.IsFailure ? result.Exception != null ? Result<T>.Failure(result.Exception) : Result<T>.Failure(result.Error) : result.Value;

    // Try methods that catch exceptions
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
    public static Result Success() => Result.Success();
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result Failure(string error) => Result.Failure(error);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
    public static Result Failure(Exception exception) => Result.Failure(exception);
    public static Result<T> Failure<T>(Exception exception) => Result<T>.Failure(exception);

    // Combine multiple results - preserving exceptions if possible
    public static Result<T> Combine<T>(this IEnumerable<Result<T>> results, Func<IEnumerable<T>, T> combiner)
    {
        var resultsList = results.ToList();
        var failures = resultsList.Where(x => x.IsFailure).ToList();

        if (failures.Count == 0) return Result<T>.Success(combiner(resultsList.Select(x => x.Value)));
        {
            // Try to find the first exception to preserve
            var firstWithException = failures.FirstOrDefault(f => f.Exception != null);
            if (firstWithException.IsFailure) return Result<T>.Failure(firstWithException.Exception!);

            // Otherwise return combined error messages
            return Result<T>.Failure(string.Join(", ", failures.Select(x => x.Error)));
        }
    }

    public static Result Combine(this IEnumerable<Result> results)
    {
        var resultsList = results.ToList();
        var failures = resultsList.Where(x => x.IsFailure).ToList();

        if (failures.Count == 0) return Result.Success();
        {
            // Try to find the first exception to preserve
            var firstWithException = failures.FirstOrDefault(f => f.Exception != null);
            if (firstWithException.IsFailure) return Result.Failure(firstWithException.Exception!);

            // Otherwise return combined error messages
            return Result.Failure(string.Join(", ", failures.Select(x => x.Error)));
        }
    }
}