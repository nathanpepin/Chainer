using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Chainer.Results;

public readonly struct Result : IResult
{
    private Result(bool isSuccess, string error, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Error = isSuccess ? string.Empty : error;
        Exception = isSuccess ? null : exception;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public Exception? Exception { get; }

    public override string ToString() => IsSuccess ? "Success" : $"Failure({Error})";

    // Factory methods
    public static Result Success() => new(true, string.Empty);

    public static Result Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty", nameof(error));

        return new Result(false, error);
    }

    public static Result Failure(Exception exception)
    {
        return new Result(false, exception.Message, exception);
    }

    // Conversions
    public static implicit operator Result(bool success) =>
        success ? Success() : Failure("Operation failed");

    public static implicit operator Result(Exception exception) =>
        Failure(exception);

    // Core methods that work with non-generic Result
    public Result Ensure(Func<bool> predicate, string error)
    {
        if (IsFailure) return this;
        return predicate() ? this : Failure(error);
    }

    public Result Ensure(Func<bool> predicate, Exception exception)
    {
        if (IsFailure) return this;
        return predicate() ? this : Failure(exception);
    }

    public async Task<Result> EnsureAsync(Func<Task<bool>> predicate, string error)
    {
        if (IsFailure) return this;
        return await predicate() ? this : Failure(error);
    }

    public async Task<Result> EnsureAsync(Func<Task<bool>> predicate, Exception exception)
    {
        if (IsFailure) return this;
        return await predicate() ? this : Failure(exception);
    }

    public Result MapError(Func<string, string> errorMapper)
    {
        if (IsSuccess) return this;
        return Failure(errorMapper(Error));
    }

    public Result MapException(Func<Exception?, Exception> exceptionMapper)
    {
        if (IsSuccess) return this;
        return Failure(exceptionMapper(Exception));
    }

    public Result Tap(Action action)
    {
        if (IsSuccess) action();
        return this;
    }

    public async Task<Result> TapAsync(Func<Task> action)
    {
        if (IsSuccess) await action();
        return this;
    }

    public Result Bind(Func<Result> binder)
    {
        return IsSuccess ? binder() : this;
    }

    public async Task<Result> BindAsync(Func<Task<Result>> binder)
    {
        return IsSuccess ? await binder() : this;
    }

    // Pattern matching
    public Result Match(Action onSuccess, Action<string, Exception?> onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure(Error, Exception);
        return this;
    }

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, Exception?, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error, Exception);
    }

    public async Task<Result> MatchAsync(Func<Task> onSuccess, Func<string, Exception?, Task> onFailure)
    {
        if (IsSuccess) await onSuccess();
        else await onFailure(Error, Exception);
        return this;
    }

    public async Task<TResult> MatchAsync<TResult>(Func<Task<TResult>> onSuccess, Func<string, Exception?, Task<TResult>> onFailure)
    {
        return IsSuccess ? await onSuccess() : await onFailure(Error, Exception);
    }

    // Try methods - simpler for backward compatibility
    public Result Match(Action onSuccess, Action<string> onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure(Error);
        return this;
    }

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetError([MaybeNullWhen(false)] out string error)
    {
        error = Error;
        return IsFailure;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetException([NotNullWhen(true)] out Exception? exception)
    {
        exception = Exception;
        return IsFailure && Exception != null;
    }
}