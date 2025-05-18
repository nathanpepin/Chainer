using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Chainer.Results;

public readonly struct Result<T> : IResult<T>
{
    private readonly T _value;

    private Result(bool isSuccess, T value, string error, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        _value = isSuccess ? value : default!;
        Error = isSuccess ? string.Empty : error;
        Exception = isSuccess ? null : exception;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public Exception? Exception { get; }

    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException($"Cannot access Value when Result is in failure state. Error: {Error}");

    public override string ToString() => IsSuccess ? $"Success({Value})" : $"Failure({Error})";

    // Factory methods
    public static Result<T> Success(T value) => new(true, value, string.Empty);

    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty", nameof(error));

        return new Result<T>(false, default!, error);
    }

    public static Result<T> Failure(Exception exception)
    {
        return new Result<T>(false, default!, exception.Message, exception);
    }

    // Implicit conversions
    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result(Result<T> result) =>
        result.IsSuccess ? Result.Success() :
        result.Exception != null ? Result.Failure(result.Exception) : Result.Failure(result.Error);

    public static implicit operator Result<T>(Exception exception) =>
        Failure(exception);

    public static implicit operator Result<T>((bool Success, T Value) tuple) =>
        tuple.Success ? Success(tuple.Value) : Failure("Operation failed");

    public static implicit operator Result<T>((bool Success, string Error, T Value) tuple) =>
        tuple.Success ? Success(tuple.Value) : Failure(tuple.Error);

    public static implicit operator Result<T>((bool Success, Exception Exception, T Value) tuple) =>
        tuple.Success ? Success(tuple.Value) : Failure(tuple.Exception);

    // Explicit conversion to T (may throw)
    public static explicit operator T(Result<T> result) => result.Value;

    // Value access methods
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue([NotNullWhen(true)] [MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return IsSuccess;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(
        [NotNullWhen(true)] [MaybeNullWhen(false)]
        out T value,
        [MaybeNullWhen(true)] out string error,
        [NotNullWhen(false)] out Exception? exception)
    {
        value = _value;
        error = Error;
        exception = Exception;
        return IsSuccess;
    }

    // Default value handling
    public T GetValueOrDefault(T defaultValue) =>
        IsSuccess ? Value : defaultValue;

    public T GetValueOrDefault(Func<T> defaultValueFactory) =>
        IsSuccess ? Value : defaultValueFactory();

    // Functional methods
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper) =>
        IsSuccess ? Result<TResult>.Success(mapper(Value)) :
        Exception != null ? Result<TResult>.Failure(Exception) : Result<TResult>.Failure(Error);

    public async Task<Result<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> mapper) =>
        IsSuccess ? Result<TResult>.Success(await mapper(Value)) :
        Exception != null ? Result<TResult>.Failure(Exception) : Result<TResult>.Failure(Error);

    public Result<T> MapError(Func<string, string> errorMapper) =>
        IsSuccess ? this : Failure(errorMapper(Error));

    public Result<T> MapException(Func<Exception?, Exception> exceptionMapper) =>
        IsSuccess ? this : Failure(exceptionMapper(Exception));

    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder) =>
        IsSuccess ? binder(Value) :
        Exception != null ? Result<TResult>.Failure(Exception) : Result<TResult>.Failure(Error);

    public async Task<Result<TResult>> BindAsync<TResult>(Func<T, Task<Result<TResult>>> binder) =>
        IsSuccess ? await binder(Value) :
        Exception != null ? Result<TResult>.Failure(Exception) : Result<TResult>.Failure(Error);

    public Result ToResult() =>
        IsSuccess ? Result.Success() :
        Exception != null ? Result.Failure(Exception) : Result.Failure(Error);

    public Result<T> Tap(Action<T> action)
    {
        if (IsSuccess) action(Value);
        return this;
    }

    public async Task<Result<T>> TapAsync(Func<T, Task> action)
    {
        if (IsSuccess) await action(Value);
        return this;
    }

    public Result<T> Ensure(Func<T, bool> predicate, string error) =>
        IsFailure ? this : predicate(Value) ? this : Failure(error);

    public Result<T> Ensure(Func<T, bool> predicate, Exception exception) =>
        IsFailure ? this : predicate(Value) ? this : Failure(exception);

    public async Task<Result<T>> EnsureAsync(Func<T, Task<bool>> predicate, string error)
    {
        if (IsFailure) return this;
        return await predicate(Value) ? this : Failure(error);
    }

    public async Task<Result<T>> EnsureAsync(Func<T, Task<bool>> predicate, Exception exception)
    {
        if (IsFailure) return this;
        return await predicate(Value) ? this : Failure(exception);
    }

    // Pattern matching with exception support
    public Result<T> Match(Action<T> onSuccess, Action<string, Exception?> onFailure)
    {
        if (IsSuccess) onSuccess(Value);
        else onFailure(Error, Exception);
        return this;
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, Exception?, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error, Exception);

    public async Task<Result<T>> MatchAsync(Func<T, Task> onSuccess, Func<string, Exception?, Task> onFailure)
    {
        if (IsSuccess) await onSuccess(Value);
        else await onFailure(Error, Exception);
        return this;
    }

    public async Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<string, Exception?, Task<TResult>> onFailure) =>
        IsSuccess ? await onSuccess(Value) : await onFailure(Error, Exception);

    // Pattern matching without exception for backward compatibility
    public Result<T> Match(Action<T> onSuccess, Action<string> onFailure)
    {
        if (IsSuccess) onSuccess(Value);
        else onFailure(Error);
        return this;
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);
}