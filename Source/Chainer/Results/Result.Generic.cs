using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Chainer.Results;

/// <summary>
/// Represents the result of an operation, which can either indicate success with a value of type <typeparamref name="T"/>
/// or failure with an associated error message and optional exception.
/// </summary>
/// <typeparam name="T">The type of the value associated with a successful result.</typeparam>
/// <remarks>
/// This struct is designed to encapsulate both successful and failed states of an operation. It provides various
/// convenience methods for error handling, functional transformations (like Map and Bind), and state matching.
/// </remarks>
public readonly struct Result<T> : IResult<T>
{
    /// <summary>
    /// Holds the encapsulated value for a successful result.
    /// Access <see cref="Value"/> to retrieve it, which throws if the result is a failure.
    /// </summary>
    private readonly T _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> struct.
    /// </summary>
    /// <param name="isSuccess">A boolean indicating if the operation was successful.</param>
    /// <param name="value">The value of the result if successful; otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <param name="error">The error message if the operation failed; otherwise, <see cref="string.Empty"/>.</param>
    /// <param name="exception">The exception associated with the failure, if any; otherwise, <c>null</c>.</param>
    private Result(bool isSuccess, T value, string error, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        _value = isSuccess ? value : default!;
        Error = isSuccess ? string.Empty : error;
        Exception = isSuccess ? null : exception;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    /// <value><c>true</c> if the operation was successful; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// This property returns <c>true</c> if the result represents a successful outcome, and <c>false</c> otherwise.
    /// It can be used to check the status of an operation.
    /// </remarks>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result represents a failure state.
    /// </summary>
    /// <value><c>true</c> if the result is a failure; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// This property returns <c>true</c> if <see cref="IsSuccess"/> is <c>false</c>.
    /// It is a convenience property for determining the failure state of the result.
    /// </remarks>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message associated with a failure result.
    /// </summary>
    /// <value>The error message if the result is a failure; otherwise, <see cref="string.Empty"/>.</value>
    /// <remarks>
    /// This property contains a descriptive error message when the operation has failed.
    /// If the result contains more detailed failure information in the form of an
    /// exception, that exception can be accessed using the <see cref="Exception"/> property.
    /// This property should primarily be checked when <see cref="IsFailure"/> is <c>true</c>.
    /// </remarks>
    public string Error { get; }

    /// <summary>
    /// Gets the exception associated with a failure result, if any.
    /// </summary>
    /// <value>The <see cref="System.Exception"/> associated with the failure, or <c>null</c> if the operation was successful, or if the failure was not caused by an exception.</value>
    /// <remarks>
    /// The associated exception provides more detailed information about the cause of the failure.
    /// </remarks>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the encapsulated value if the result represents a successful operation.
    /// </summary>
    /// <value>The value of type <typeparamref name="T"/> if the result is successful.</value>
    /// <exception cref="InvalidOperationException">Thrown if accessed when the result is in a failure state. The exception message includes the <see cref="Error"/>.</exception>
    /// <remarks>
    /// Before accessing this property, it's recommended to check <see cref="IsSuccess"/>.
    /// If the result is a failure, <see cref="Error"/> or <see cref="Exception"/> properties can provide more details.
    /// </remarks>
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException($"Cannot access Value when Result is in failure state. Error: {Error}");

    /// <summary>
    /// Converts the result to its string representation.
    /// </summary>
    /// <returns>
    /// A string representation of the result. If successful, "Success(Value)"; if a failure, "Failure(Error)".
    /// </returns>
    public override string ToString()
    {
        return IsSuccess ? $"Success({Value})" : $"Failure({Error})";
    }

    // Factory methods
    /// <summary>
    /// Creates a successful <see cref="Result{T}"/> with the specified value.
    /// </summary>
    /// <param name="value">The value to associate with the successful result.</param>
    /// <returns>A <see cref="Result{T}"/> instance indicating a successful operation, containing the provided <paramref name="value"/>.</returns>
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, string.Empty);
    }

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/> with the specified error message.
    /// The value of the result will be the default for type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="error">The error message describing the failure. Cannot be null, empty, or whitespace.</param>
    /// <returns>A <see cref="Result{T}"/> instance indicating a failure, containing the specified <paramref name="error"/> message.</returns>
    /// <exception cref="ArgumentException">Thrown if the provided <paramref name="error"/> message is null, empty, or consists only of whitespace.</exception>
    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty", nameof(error));

        return new Result<T>(false, default!, error);
    }

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/> from the specified exception.
    /// The exception's message is used as the error message, and the exception itself is stored.
    /// The value of the result will be the default for type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="exception">The exception that represents the reason for the failure. Cannot be null.</param>
    /// <returns>A <see cref="Result{T}"/> instance indicating a failure, containing the error message and exception from the provided <paramref name="exception"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="exception"/> is null.</exception>
    public static Result<T> Failure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new Result<T>(false, default!, exception.Message, exception);
    }

    // Implicit conversions
    /// <summary>
    /// Defines an implicit conversion from a value of type <typeparamref name="T"/> to a successful <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="value">The value to wrap in a successful <see cref="Result{T}"/>.</param>
    /// <returns>A <see cref="Result{T}"/> instance indicating success, containing the provided <paramref name="value"/>.</returns>
    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }

    /// <summary>
    /// Defines an implicit conversion from a <see cref="Result{T}"/> to a non-generic <see cref="Result"/>.
    /// If the <see cref="Result{T}"/> is successful, a successful non-generic <see cref="Result"/> is returned.
    /// If the <see cref="Result{T}"/> is a failure, a failed non-generic <see cref="Result"/> is returned, preserving the error message or exception.
    /// </summary>
    /// <param name="result">The <see cref="Result{T}"/> instance to convert.</param>
    /// <returns>A non-generic <see cref="Result"/> representing the same success or failure state.</returns>
    public static implicit operator Result(Result<T> result)
    {
        return result.IsSuccess ? Result.Success() :
            result.Exception != null ? Result.Failure(result.Exception) : Result.Failure(result.Error);
    }

    /// <summary>
    /// Defines an implicit conversion from an <see cref="Exception"/> to a failed <see cref="Result{T}"/>.
    /// The exception's message will be used as the <see cref="Error"/> property.
    /// </summary>
    /// <param name="exception">The exception to convert into a failed <see cref="Result{T}"/>. Cannot be null.</param>
    /// <returns>A failed <see cref="Result{T}"/> instance containing the error message and exception from the provided <paramref name="exception"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="exception"/> is null.</exception>
    public static implicit operator Result<T>(Exception exception)
    {
        return Failure(exception);
    }

    /// <summary>
    /// Defines an implicit conversion from a tuple (<see cref="bool"/> Success, <typeparamref name="T"/> Value) to a <see cref="Result{T}"/>.
    /// If `tuple.Success` is <c>true</c>, a successful <see cref="Result{T}"/> with `tuple.Value` is returned.
    /// If `tuple.Success` is <c>false</c>, a failed <see cref="Result{T}"/> with a default error message "Operation failed" is returned.
    /// </summary>
    /// <param name="tuple">A tuple containing a success flag and a value.</param>
    /// <returns>A <see cref="Result{T}"/> based on the tuple's success flag and value.</returns>
    public static implicit operator Result<T>((bool Success, T Value) tuple)
    {
        return tuple.Success ? Success(tuple.Value) : Failure("Operation failed");
    }

    /// <summary>
    /// Defines an implicit conversion from a tuple (<see cref="bool"/> Success, <see cref="string"/> Error, <typeparamref name="T"/> Value) to a <see cref="Result{T}"/>.
    /// If `tuple.Success` is <c>true</c>, a successful <see cref="Result{T}"/> with `tuple.Value` is returned.
    /// If `tuple.Success` is <c>false</c>, a failed <see cref="Result{T}"/> with `tuple.Error` is returned.
    /// </summary>
    /// <param name="tuple">A tuple containing a success flag, an error message, and a value.</param>
    /// <returns>A <see cref="Result{T}"/> based on the tuple's components.</returns>
    /// <exception cref="ArgumentException">Thrown if `tuple.Success` is <c>false</c> and `tuple.Error` is null or whitespace.</exception>
    public static implicit operator Result<T>((bool Success, string Error, T Value) tuple)
    {
        return tuple.Success ? Success(tuple.Value) : Failure(tuple.Error);
    }

    /// <summary>
    /// Defines an implicit conversion from a tuple (<see cref="bool"/> Success, <see cref="Exception"/> Exception, <typeparamref name="T"/> Value) to a <see cref="Result{T}"/>.
    /// If `tuple.Success` is <c>true</c>, a successful <see cref="Result{T}"/> with `tuple.Value` is returned.
    /// If `tuple.Success` is <c>false</c>, a failed <see cref="Result{T}"/> created from `tuple.Exception` is returned.
    /// </summary>
    /// <param name="tuple">A tuple containing a success flag, an exception, and a value.</param>
    /// <returns>A <see cref="Result{T}"/> based on the tuple's components.</returns>
    /// <exception cref="ArgumentNullException">Thrown if `tuple.Success` is <c>false</c> and `tuple.Exception` is null.</exception>
    public static implicit operator Result<T>((bool Success, Exception Exception, T Value) tuple)
    {
        return tuple.Success ? Success(tuple.Value) : Failure(tuple.Exception);
    }

    // Explicit conversion to T (may throw)
    /// <summary>
    /// Defines an explicit conversion from a <see cref="Result{T}"/> instance to its underlying value of type <typeparamref name="T"/>.
    /// This conversion will succeed only if <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    /// <param name="result">The <see cref="Result{T}"/> instance to convert.</param>
    /// <returns>The underlying value of type <typeparamref name="T"/> if the result is successful.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="IsSuccess"/> is <c>false</c> (i.e., an attempt is made to access the value of a failed result).</exception>
    public static explicit operator T(Result<T> result)
    {
        return result.Value;
    }

    // Value access methods
    /// <summary>
    /// Attempts to retrieve the value if the result is successful.
    /// </summary>
    /// <param name="value">When this method returns, contains the value of type <typeparamref name="T"/> if the result was successful, or the default value of <typeparamref name="T"/> if it was a failure. This parameter is passed uninitialized.</param>
    /// <returns><c>true</c> if the result was successful and <paramref name="value"/> was set; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue([NotNullWhen(true)] [MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return IsSuccess;
    }

    /// <summary>
    /// Attempts to retrieve the error message if the result is a failure.
    /// </summary>
    /// <param name="error">When this method returns, contains the error message if the result was a failure; otherwise, <c>null</c> (or <see cref="string.Empty"/> if failure had no specific message but was not a success). This parameter is passed uninitialized.</param>
    /// <returns><c>true</c> if the result was a failure; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetError([MaybeNullWhen(false)] out string error)
    {
        error = Error; // Will be string.Empty if IsSuccess is true
        return IsFailure;
    }

    /// <summary>
    /// Attempts to retrieve the exception if the result is a failure and an exception is present.
    /// </summary>
    /// <param name="exception">When this method returns, contains the <see cref="System.Exception"/> if the result was a failure and an exception was provided; otherwise, <c>null</c>. This parameter is passed uninitialized.</param>
    /// <returns><c>true</c> if the result was a failure and an exception is present; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetException([NotNullWhen(true)] out Exception? exception)
    {
        exception = Exception;
        return IsFailure && Exception != null;
    }

    /// <summary>
    /// Attempts to retrieve the value if the result is successful, or the error details if it's a failure.
    /// </summary>
    /// <param name="value">When this method returns <c>true</c>, contains the successful value. When it returns <c>false</c>, contains the default value of <typeparamref name="T"/>. This parameter is passed uninitialized.</param>
    /// <param name="error">When this method returns <c>false</c>, contains the error message if the result was a failure. When it returns <c>true</c>, contains <c>null</c> (or <see cref="string.Empty"/>). This parameter is passed uninitialized.</param>
    /// <param name="exception">When this method returns <c>false</c>, contains the <see cref="System.Exception"/> if the result was a failure and an exception was provided. When it returns <c>true</c>, or if no exception was associated with the failure, contains <c>null</c>. This parameter is passed uninitialized.</param>
    /// <returns><c>true</c> if the result was successful; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(
        [NotNullWhen(true)] [MaybeNullWhen(false)]
        out T value,
        [MaybeNullWhen(true)] out string error, // Error is null/empty on success
        out Exception? exception) // Exception is null on success or if failure has no exception
    {
        value = _value;    // Contains actual value on success, default(T) on failure (due to struct initialization)
        error = Error;     // Contains actual error on failure, string.Empty on success
        exception = Exception; // Contains actual exception on failure (if any), null on success

        return IsSuccess;
    }


    // Default value handling
    /// <summary>
    /// Gets the value if the result is successful; otherwise, returns the specified <paramref name="defaultValue"/>.
    /// </summary>
    /// <param name="defaultValue">The value to return if the result is a failure.</param>
    /// <returns>The result's value if successful; otherwise, <paramref name="defaultValue"/>.</returns>
    public T GetValueOrDefault(T defaultValue)
    {
        return IsSuccess ? Value : defaultValue;
    }

    /// <summary>
    /// Gets the value if the result is successful; otherwise, returns the value provided by the <paramref name="defaultValueFactory"/> function.
    /// </summary>
    /// <param name="defaultValueFactory">A function that produces a default value if the result is a failure. This function is only executed if needed.</param>
    /// <returns>The result's value if successful; otherwise, the value returned by <paramref name="defaultValueFactory"/>.</returns>
    public T GetValueOrDefault(Func<T> defaultValueFactory)
    {
        return IsSuccess ? Value : defaultValueFactory();
    }

    // Functional methods
    /// <summary>
    /// If successful, transforms the encapsulated value of type <typeparamref name="T"/> to a new value of type <typeparamref name="TResult"/> using the <paramref name="mapper"/> function.
    /// If a failure, the failure (error message and exception) is propagated to the new <see cref="Result{TResult}"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the value in the resulting <see cref="Result{TResult}"/>.</typeparam>
    /// <param name="mapper">A function to apply to the successful value.</param>
    /// <returns>A <see cref="Result{TResult}"/> which is successful with the mapped value if the original result was successful, or a failure propagating the original error details.</returns>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return IsSuccess ? Result<TResult>.Success(mapper(Value)) :
            Exception != null ? Result<TResult>.Failure(Exception) : Result<TResult>.Failure(Error);
    }

    /// <summary>
    /// If successful, asynchronously transforms the encapsulated value of type <typeparamref name="T"/> to a new value of type <typeparamref name="TResult"/> using the asynchronous <paramref name="mapper"/> function.
    /// If a failure, the failure (error message and exception) is propagated to the new <see cref="Result{TResult}"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the value in the resulting <see cref="Result{TResult}"/>.</typeparam>
    /// <param name="mapper">An asynchronous function to apply to the successful value.</param>
    /// <returns>A <see cref="Task{T}"/> that will yield a <see cref="Result{TResult}"/> which is successful with the mapped value if the original result was successful, or a failure propagating the original error details.</returns>
    public async Task<Result<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> mapper)
    {
        return IsSuccess ? Result<TResult>.Success(await mapper(Value).ConfigureAwait(false)) :
            Exception != null ? Result<TResult>.Failure(Exception) : Result<TResult>.Failure(Error);
    }

    /// <summary>
    /// If the result is a failure, maps its <see cref="Error"/> message to a new error message using the specified <paramref name="errorMapper"/> function.
    /// If the result is successful, it's returned unchanged.
    /// </summary>
    /// <param name="errorMapper">A function that takes the current error message and returns a new error message.</param>
    /// <returns>
    /// A new failure <see cref="Result{T}"/> with the mapped error message if the current result is a failure;
    /// otherwise, the original successful <see cref="Result{T}"/>.
    /// </returns>
    public Result<T> MapError(Func<string, string> errorMapper)
    {
        return IsSuccess ? this : Failure(errorMapper(Error));
    }

    /// <summary>
    /// If the result is a failure, maps its <see cref="Exception"/> (if any) to a new exception using the specified <paramref name="exceptionMapper"/> function.
    /// The new failure <see cref="Result{T}"/> will use the message of the mapped exception as its error message.
    /// If the result is successful, it's returned unchanged.
    /// </summary>
    /// <param name="exceptionMapper">A function that transforms the current exception (or <c>null</c> if none) into a new exception.</param>
    /// <returns>
    /// A new failure <see cref="Result{T}"/> with the mapped exception if the current result is a failure;
    /// otherwise, the original successful <see cref="Result{T}"/>.
    /// </returns>
    public Result<T> MapException(Func<Exception?, Exception> exceptionMapper)
    {
        return IsSuccess ? this : Failure(exceptionMapper(Exception));
    }

    /// <summary>
    /// If successful, applies the <paramref name="binder"/> function to the encapsulated value, returning the <see cref="Result{TResult}"/> produced by the binder.
    /// If a failure, the failure (error message and exception) is propagated to the new <see cref="Result{TResult}"/>.
    /// This is used for chaining operations where each operation returns a <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the value in the <see cref="Result{TResult}"/> returned by the <paramref name="binder"/> function.</typeparam>
    /// <param name="binder">A function that takes the successful value and returns a <see cref="Result{TResult}"/>.</param>
    /// <returns>The <see cref="Result{TResult}"/> returned by <paramref name="binder"/> if the original result was successful; otherwise, a failure <see cref="Result{TResult}"/> propagating the original error details.</returns>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
    {
        return IsSuccess ? binder(Value) :
            Exception != null ? Result<TResult>.Failure(Exception) : Result<TResult>.Failure(Error);
    }

    /// <summary>
    /// If successful, applies the asynchronous <paramref name="binder"/> function to the encapsulated value, returning the <see cref="Result{TResult}"/> produced by the binder.
    /// If a failure, the failure (error message and exception) is propagated to the new <see cref="Result{TResult}"/>.
    /// This is used for chaining asynchronous operations where each operation returns a <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the value in the <see cref="Result{TResult}"/> returned by the <paramref name="binder"/> function.</typeparam>
    /// <param name="binder">An asynchronous function that takes the successful value and returns a <see cref="Task{T}"/> of <see cref="Result{TResult}"/>.</param>
    /// <returns>A <see cref="Task{T}"/> that will yield the <see cref="Result{TResult}"/> returned by <paramref name="binder"/> if the original result was successful; otherwise, a failure <see cref="Result{TResult}"/> propagating the original error details.</returns>
    public async Task<Result<TResult>> BindAsync<TResult>(Func<T, Task<Result<TResult>>> binder)
    {
        return IsSuccess ? await binder(Value).ConfigureAwait(false) :
            Exception != null ? Result<TResult>.Failure(Exception) : Result<TResult>.Failure(Error);
    }

    /// <summary>
    /// Converts the current <see cref="Result{T}"/> to a non-generic <see cref="Result"/>.
    /// If successful, returns <see cref="Results.Result.Success()"/>.
    /// If a failure, returns a <see cref="Results.Result.Failure(string)"/> or <see cref="Results.Result.Failure(Exception)"/>, preserving the error details.
    /// </summary>
    /// <returns>A non-generic <see cref="Result"/> representing the success or failure state of this instance.</returns>
    public Result ToResult()
    {
        return IsSuccess ? Result.Success() :
            Exception != null ? Result.Failure(Exception) : Result.Failure(Error);
    }

    /// <summary>
    /// Executes the specified <paramref name="action"/> with the encapsulated value if the <see cref="Result{T}"/> is successful.
    /// This method allows for "tapping into" the success path to perform side effects without altering the result.
    /// </summary>
    /// <param name="action">The action to execute with the successful value.</param>
    /// <returns>The original <see cref="Result{T}"/> instance, allowing for fluent chaining.</returns>
    public Result<T> Tap(Action<T> action)
    {
        if (IsSuccess) action(Value);
        return this;
    }

    /// <summary>
    /// Executes the specified asynchronous <paramref name="action"/> with the encapsulated value if the <see cref="Result{T}"/> is successful.
    /// This method allows for "tapping into" the success path to perform asynchronous side effects without altering the result.
    /// </summary>
    /// <param name="action">The asynchronous action (returning a <see cref="Task"/>) to execute with the successful value.</param>
    /// <returns>A <see cref="Task{T}"/> that will yield the original <see cref="Result{T}"/> instance, allowing for fluent chaining.</returns>
    public async Task<Result<T>> TapAsync(Func<T, Task> action)
    {
        if (IsSuccess) await action(Value).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Ensures that the successful value satisfies the specified <paramref name="predicate"/>.
    /// If the <see cref="Result{T}"/> is already a failure, it's returned as is.
    /// If the <see cref="Result{T}"/> is successful but the value fails the <paramref name="predicate"/>, a new failure <see cref="Result{T}"/> is returned with the given <paramref name="error"/> message.
    /// </summary>
    /// <param name="predicate">The function to evaluate against the successful value. Must return <c>true</c> for the condition to be satisfied.</param>
    /// <param name="error">The error message to use if the <paramref name="predicate"/> returns <c>false</c>.</param>
    /// <returns>The original <see cref="Result{T}"/> if it's a failure or if it's successful and satisfies the <paramref name="predicate"/>; otherwise, a new failure <see cref="Result{T}"/>.</returns>
    public Result<T> Ensure(Func<T, bool> predicate, string error)
    {
        return IsFailure ? this : predicate(Value) ? this : Failure(error);
    }

    /// <summary>
    /// Ensures that the successful value satisfies the specified <paramref name="predicate"/>.
    /// If the <see cref="Result{T}"/> is already a failure, it's returned as is.
    /// If the <see cref="Result{T}"/> is successful but the value fails the <paramref name="predicate"/>, a new failure <see cref="Result{T}"/> is returned, created from the given <paramref name="exception"/>.
    /// </summary>
    /// <param name="predicate">The function to evaluate against the successful value. Must return <c>true</c> for the condition to be satisfied.</param>
    /// <param name="exception">The exception to use for creating the failure <see cref="Result{T}"/> if the <paramref name="predicate"/> returns <c>false</c>.</param>
    /// <returns>The original <see cref="Result{T}"/> if it's a failure or if it's successful and satisfies the <paramref name="predicate"/>; otherwise, a new failure <see cref="Result{T}"/>.</returns>
    public Result<T> Ensure(Func<T, bool> predicate, Exception exception)
    {
        return IsFailure ? this : predicate(Value) ? this : Failure(exception);
    }

    /// <summary>
    /// Ensures that the successful value satisfies the specified asynchronous <paramref name="predicate"/>.
    /// If the <see cref="Result{T}"/> is already a failure, it's returned as is.
    /// If the <see cref="Result{T}"/> is successful but the value fails the asynchronous <paramref name="predicate"/>, a new failure <see cref="Result{T}"/> is returned with the given <paramref name="error"/> message.
    /// </summary>
    /// <param name="predicate">The asynchronous function (returning a <see cref="Task{Boolean}"/>) to evaluate against the successful value. Must return <c>true</c> for the condition to be satisfied.</param>
    /// <param name="error">The error message to use if the <paramref name="predicate"/> returns <c>false</c>.</param>
    /// <returns>A <see cref="Task{T}"/> that will yield the original <see cref="Result{T}"/> if it's a failure or if it's successful and satisfies the <paramref name="predicate"/>; otherwise, a new failure <see cref="Result{T}"/>.</returns>
    public async Task<Result<T>> EnsureAsync(Func<T, Task<bool>> predicate, string error)
    {
        if (IsFailure) return this;
        return await predicate(Value).ConfigureAwait(false) ? this : Failure(error);
    }

    /// <summary>
    /// Ensures that the successful value satisfies the specified asynchronous <paramref name="predicate"/>.
    /// If the <see cref="Result{T}"/> is already a failure, it's returned as is.
    /// If the <see cref="Result{T}"/> is successful but the value fails the asynchronous <paramref name="predicate"/>, a new failure <see cref="Result{T}"/> is returned, created from the given <paramref name="exception"/>.
    /// </summary>
    /// <param name="predicate">The asynchronous function (returning a <see cref="Task{Boolean}"/>) to evaluate against the successful value. Must return <c>true</c> for the condition to be satisfied.</param>
    /// <param name="exception">The exception to use for creating the failure <see cref="Result{T}"/> if the <paramref name="predicate"/> returns <c>false</c>.</param>
    /// <returns>A <see cref="Task{T}"/> that will yield the original <see cref="Result{T}"/> if it's a failure or if it's successful and satisfies the <paramref name="predicate"/>; otherwise, a new failure <see cref="Result{T}"/>.</returns>
    public async Task<Result<T>> EnsureAsync(Func<T, Task<bool>> predicate, Exception exception)
    {
        if (IsFailure) return this;
        return await predicate(Value).ConfigureAwait(false) ? this : Failure(exception);
    }

    // Pattern matching with exception support
    /// <summary>
    /// Executes one of the provided actions based on whether the <see cref="Result{T}"/> is a success or a failure.
    /// </summary>
    /// <param name="onSuccess">The action to execute if the <see cref="Result{T}"/> is successful. It receives the successful value.</param>
    /// <param name="onFailure">The action to execute if the <see cref="Result{T}"/> is a failure. It receives the error message and the (optional) exception.</param>
    /// <returns>The original <see cref="Result{T}"/> instance, allowing for method chaining if needed.</returns>
    public Result<T> Match(Action<T> onSuccess, Action<string, Exception?> onFailure)
    {
        if (IsSuccess) onSuccess(Value);
        else onFailure(Error, Exception);
        return this;
    }

    /// <summary>
    /// Executes one of the provided functions based on whether the <see cref="Result{T}"/> is a success or a failure, and returns its result.
    /// </summary>
    /// <typeparam name="TResult">The type of the value to be returned by the match functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the <see cref="Result{T}"/> is successful. It receives the successful value and returns a <typeparamref name="TResult"/>.</param>
    /// <param name="onFailure">The function to execute if the <see cref="Result{T}"/> is a failure. It receives the error message and the (optional) exception, and returns a <typeparamref name="TResult"/>.</param>
    /// <returns>The value returned by either the <paramref name="onSuccess"/> or <paramref name="onFailure"/> function.</returns>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, Exception?, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error, Exception);
    }

    /// <summary>
    /// Asynchronously executes one of the provided asynchronous actions based on whether the <see cref="Result{T}"/> is a success or a failure.
    /// </summary>
    /// <param name="onSuccess">The asynchronous action (returning a <see cref="Task"/>) to execute if the <see cref="Result{T}"/> is successful. It receives the successful value.</param>
    /// <param name="onFailure">The asynchronous action (returning a <see cref="Task"/>) to execute if the <see cref="Result{T}"/> is a failure. It receives the error message and the (optional) exception.</param>
    /// <returns>A <see cref="Task{T}"/> that will yield the original <see cref="Result{T}"/> instance after the asynchronous action completes.</returns>
    public async Task<Result<T>> MatchAsync(Func<T, Task> onSuccess, Func<string, Exception?, Task> onFailure)
    {
        if (IsSuccess) await onSuccess(Value).ConfigureAwait(false);
        else await onFailure(Error, Exception).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Asynchronously executes one of the provided asynchronous functions based on whether the <see cref="Result{T}"/> is a success or a failure, and returns its result.
    /// </summary>
    /// <typeparam name="TResult">The type of the value to be returned by the match functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function (returning a <see cref="Task{TResult}"/> of <typeparamref name="TResult"/>) to execute if the <see cref="Result{T}"/> is successful. It receives the successful value.</param>
    /// <param name="onFailure">The asynchronous function (returning a <see cref="Task{TResult}"/> of <typeparamref name="TResult"/>) to execute if the <see cref="Result{T}"/> is a failure. It receives the error message and the (optional) exception.</param>
    /// <returns>A <see cref="Task{TResult}"/> that will yield the value returned by either the <paramref name="onSuccess"/> or <paramref name="onFailure"/> asynchronous function.</returns>
    public async Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<string, Exception?, Task<TResult>> onFailure)
    {
        return IsSuccess ? await onSuccess(Value).ConfigureAwait(false) : await onFailure(Error, Exception).ConfigureAwait(false);
    }

    // Pattern matching without exception for backward compatibility
    /// <summary>
    /// Executes one of the provided actions based on whether the <see cref="Result{T}"/> is a success or a failure.
    /// This overload of Match provides an <paramref name="onFailure"/> action that only accepts the error message.
    /// </summary>
    /// <param name="onSuccess">The action to execute if the <see cref="Result{T}"/> is successful. It receives the successful value.</param>
    /// <param name="onFailure">The action to execute if the <see cref="Result{T}"/> is a failure. It receives the error message.</param>
    /// <returns>The original <see cref="Result{T}"/> instance, allowing for method chaining.</returns>
    public Result<T> Match(Action<T> onSuccess, Action<string> onFailure)
    {
        if (IsSuccess) onSuccess(Value);
        else onFailure(Error);
        return this;
    }

    /// <summary>
    /// Executes one of the provided functions based on whether the <see cref="Result{T}"/> is a success or a failure, and returns its result.
    /// This overload of Match provides an <paramref name="onFailure"/> function that only accepts the error message.
    /// </summary>
    /// <typeparam name="TResult">The type of the value to be returned by the match functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the <see cref="Result{T}"/> is successful. It receives the successful value and returns a <typeparamref name="TResult"/>.</param>
    /// <param name="onFailure">The function to execute if the <see cref="Result{T}"/> is a failure. It receives the error message and returns a <typeparamref name="TResult"/>.</param>
    /// <returns>The value returned by either the <paramref name="onSuccess"/> or <paramref name="onFailure"/> function.</returns>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}