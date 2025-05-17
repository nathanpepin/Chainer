namespace Chainer.Results;

public static class ResultExtensions
{
    public static Result<T> Flatten<T>(this Result<Result<T>> it)
    {
        return it.IsFailure ? Failure<T>(it.Error) : it.Value;
    }

    // Try methods - catch exceptions and convert to Result
    public static Result Try(Action action)
    {
        try
        {
            action();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
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
            return Result.Failure(ex.Message);
        }
    }

    public static Result<T> Try<T>(Func<T> func)
    {
        try
        {
            return Result.Success(func());
        }
        catch (Exception ex)
        {
            return Result.Failure<T>(ex.Message);
        }
    }

    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> func)
    {
        try
        {
            return Result.Success(await func());
        }
        catch (Exception ex)
        {
            return Result.Failure<T>(ex.Message);
        }
    }

    // Create success Result directly
    public static Result Success()
    {
        return Result.Success();
    }

    public static Result<T> Success<T>(T value)
    {
        return Result.Success(value);
    }

    // Create failure Result directly
    public static Result Failure(string error)
    {
        return Result.Failure(error);
    }

    public static Result<T> Failure<T>(string error)
    {
        return Result.Failure<T>(error);
    }

    public static Result Failure(Exception exception)
    {
        return Result.Failure(exception.Message);
    }

    public static Result<T> Failure<T>(Exception exception)
    {
        return Result.Failure<T>(exception.Message);
    }

    // Existing extension methods
    public static Result<TOut> OnSuccess<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> func)
    {
        return result.Map(func);
    }

    public static Result<TIn> OnSuccess<TIn>(this Result<TIn> result, Action<TIn> action)
    {
        return result.Tap(action);
    }

    public static Result<TIn> OnFailure<TIn>(this Result<TIn> result, Action<string> action)
    {
        if (result.IsFailure)
            action(result.Error);

        return result;
    }

    public static Result OnSuccess(this Result result, Action action)
    {
        if (result.IsSuccess)
            action();

        return result;
    }

    public static Result OnFailure(this Result result, Action<string> action)
    {
        if (result.IsFailure)
            action(result.Error);

        return result;
    }

    public static Result<T> Combine<T>(this IEnumerable<Result<T>> results, Func<IEnumerable<T>, T> combiner)
    {
        var resultsList = results.ToList();

        var failures = resultsList
            .Where(x => x.IsFailure)
            .Select(x => x.Error)
            .ToList();

        if (failures.Any())
            return Result.Failure<T>(string.Join(", ", failures));

        var values = resultsList.Select(x => x.Value);
        return Result.Success(combiner(values));
    }

    public static Result Combine(this IEnumerable<Result> results)
    {
        var resultsList = results.ToList();

        var failures = resultsList
            .Where(x => x.IsFailure)
            .Select(x => x.Error)
            .ToList();

        if (failures.Any())
            return Result.Failure(string.Join(", ", failures));

        return Result.Success();
    }
}