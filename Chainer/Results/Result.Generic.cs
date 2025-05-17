using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Chainer.Results;

public readonly struct Result<T> : IResult<T>
{
    private readonly T _value;

    private Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        _value = isSuccess ? value : default!;
        Error = isSuccess ? string.Empty : error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException($"Cannot access Value when Result is in failure state. Error: {Error}");

    public override string ToString()
    {
        return IsSuccess
            ? $"Success({Value})"
            : $"Failure({Error})";
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, string.Empty);
    }

    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty", nameof(error));

        return new Result<T>(false, default!, error);
    }

    // Implicit conversion from T to Result<T>
    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }

    // Implicit conversion from Result<T> to Result
    public static implicit operator Result(Result<T> result)
    {
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    // Implicit conversion from Exception to Result<T>
    public static implicit operator Result<T>(Exception exception)
    {
        return Failure(exception.Message);
    }

    // Implicit conversion from (bool, T) tuple to Result<T>
    public static implicit operator Result<T>((bool Success, T Value) tuple)
    {
        return tuple.Success ? Success(tuple.Value) : Failure("Operation failed");
    }

    // Implicit conversion from (bool, string, T) tuple to Result<T>
    public static implicit operator Result<T>((bool Success, string Error, T Value) tuple)
    {
        return tuple.Success ? Success(tuple.Value) : Failure(tuple.Error);
    }

    // Explicit conversion from Result<T> to T (may throw if Result is failure)
    public static explicit operator T(Result<T> result)
    {
        return result.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue([NotNullWhen(true)] [MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return IsSuccess;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetError([NotNullWhen(true)] [MaybeNullWhen(false)] out string error)
    {
        error = Error;
        return IsFailure;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue([NotNullWhen(true)] [MaybeNullWhen(false)] out T value, [NotNullWhen(false)] [MaybeNullWhen(true)] out string error)
    {
        value = _value;
        error = Error;
        return IsSuccess;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetError([NotNullWhen(true)] [MaybeNullWhen(false)] out string error, [NotNullWhen(false)] [MaybeNullWhen(true)] out T value)
    {
        value = _value;
        error = Error;
        return IsFailure;
    }

    public T GetValueOrDefault(T defaultValue)
    {
        return IsSuccess ? Value : defaultValue;
    }

    public T GetValueOrDefault(Func<T> defaultValueFactory)
    {
        return IsSuccess ? Value : defaultValueFactory();
    }

    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return IsSuccess ? Result.Success(mapper(Value)) : Result.Failure<TResult>(Error);
    }

    public async Task<Result<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> mapper)
    {
        return IsSuccess ? Result.Success(await mapper(Value)) : Result.Failure<TResult>(Error);
    }

    public Result<T> MapError(Func<string, string> errorMapper)
    {
        return IsSuccess ? this : Failure(errorMapper(Error));
    }

    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
    {
        return IsSuccess ? binder(Value) : Result.Failure<TResult>(Error);
    }

    public async Task<Result<TResult>> BindAsync<TResult>(Func<T, Task<Result<TResult>>> binder)
    {
        return IsSuccess ? await binder(Value) : Result.Failure<TResult>(Error);
    }

    public Result ToResult()
    {
        return IsSuccess ? Result.Success() : Result.Failure(Error);
    }

    public Result<T> Tap(Action<T> action)
    {
        if (IsSuccess)
            action(Value);

        return this;
    }

    public async Task<Result<T>> TapAsync(Func<T, Task> action)
    {
        if (IsSuccess)
            await action(Value);

        return this;
    }

    public Result<T> Ensure(Func<T, bool> predicate, string error)
    {
        if (IsFailure) return this;
        return predicate(Value) ? this : Failure(error);
    }

    public Result<T> Match(Action<T> onSuccess, Action<string> onFailure)
    {
        if (IsSuccess)
            onSuccess(Value);
        else
            onFailure(Error);

        return this;
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}