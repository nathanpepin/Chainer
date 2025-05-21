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
    /// Holds the encapsulated value associated with a successful result.
    /// This property contains the value returned when the result indicates
    /// success. It is inaccessible if the result indicates failure.
    /// </summary>
    private readonly T _value;

    /// <summary>
    /// Represents the outcome of an operation that can either succeed or fail, providing additional
    /// information about the result, such as a value, error message, or exception related to the operation.
    /// </summary>
    /// <typeparam name="T">The type of the value associated with a successful operation.</typeparam>
    private Result(bool isSuccess, T value, string error, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        _value = isSuccess ? value : default!;
        Error = isSuccess ? string.Empty : error;
        Exception = isSuccess ? null : exception;
    }

    /// Indicates whether the operation was successful.
    /// This property returns true if the result represents a successful outcome, and false otherwise.
    /// Can be used to check the status of an operation or process within a result context.
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result represents a failure state.
    /// </summary>
    /// <remarks>
    /// This property returns true if the result is not successful, meaning <see cref="IsSuccess"/> is false.
    /// It is a convenience property for determining the failure state of the result.
    /// </remarks>
    public bool IsFailure => !IsSuccess;

    /// Gets the error message associated with a failure result.
    /// This property contains a descriptive error message when the operation
    /// represented by the result has failed. It will be an empty string
    /// if the result indicates success.
    /// If the result contains more detailed failure information in the form of an
    /// exception, that exception can be accessed using the `Exception` property.
    /// This property should be checked or used only when the result is in a failure
    /// state, as indicated by the `IsFailure` property.
    public string Error { get; }

    /// Gets the exception associated with the failure of the result.
    /// This property is null when the result is in a successful state or if the failure
    /// was caused by an error message and not an exception.
    /// The associated exception, if any, provides more detailed information about the
    /// cause of the failure. Use this property as an additional diagnostic tool to handle
    /// or log specific error scenarios appropriately.
    public Exception? Exception { get; }

    /// Gets the encapsulated value if the result represents a successful operation.
    /// Throws an InvalidOperationException if accessed when the result is in a failure state.
    /// If the result is a failure, additional information regarding the failure,
    /// such as error message or exception, can be retrieved from the Error or Exception properties.
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException($"Cannot access Value when Result is in failure state. Error: {Error}");

    /// Converts the result to its string representation.
    /// <return>
    /// A string containing the representation of the result. If the result is successful,
    /// it returns "Success({Value})", where {Value} is the value of the result. If the
    /// result is a failure, it returns "Failure({Error})", where {Error} is the error description.
    /// </return>
    public override string ToString()
    {
        return IsSuccess ? $"Success({Value})" : $"Failure({Error})";
    }

    // Factory methods
    /// Represents a result of an operation that succeeded.
    /// <param name="value">The value of type T returned by the successfully completed operation.</param>
    /// <returns>A result object indicating a successful operation containing the value provided.</returns>
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, string.Empty);
    }

    /// Creates a new failure result with the specified error message.
    /// <param name="error">
    /// The error message associated with the failure result. Cannot be null, empty, or whitespace.
    /// </param>
    /// <returns>
    /// A failure result containing the specified error message.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the provided error message is null, empty, or consists only of whitespace.
    /// </exception>
    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty", nameof(error));

        return new Result<T>(false, default!, error);
    }

    /// Creates a failure result with an exception.
    /// <param name="exception">The exception that represents the reason for the failure.</param>
    /// <return>A failure result containing the specified exception.</return>
    public static Result<T> Failure(Exception exception)
    {
        return new Result<T>(false, default!, exception.Message, exception);
    }

    // Implicit conversions
    /// Defines a custom operator for a class or struct, enabling user-defined behavior for specific operator functionality.
    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }

    /// Defines an implicit conversion operator for converting a Result{T} to a Result.
    /// This operator enables the conversion of successful and failed results
    /// from Result{T} to a non-generic Result type. If the Result{T} is a success,
    /// a successful Result is returned. If the Result{T} is a failure, the resulting
    /// error or exception is used to construct a failed Result.
    /// The operator handles the following cases:
    /// - If the Result{T} is successful, a successful Result is returned.
    /// - If the Result{T} is a failure with an associated exception, a failed Result is
    /// created with that exception.
    /// - If the Result{T} is a failure with an error message, a failed Result is created
    /// with that error message.
    public static implicit operator Result(Result<T> result)
    {
        return result.IsSuccess ? Result.Success() :
            result.Exception != null ? Result.Failure(result.Exception) : Result.Failure(result.Error);
    }

    /// Defines a custom operator for a class or struct.
    /// Allows for the implementation of operations such as addition, subtraction,
    /// comparison, or other custom behaviors specific to the type.
    public static implicit operator Result<T>(Exception exception)
    {
        return Failure(exception);
    }

    /// Defines a custom implementation for an operator, allowing specific behavior
    /// when the operator is used with the containing type.
    public static implicit operator Result<T>((bool Success, T Value) tuple)
    {
        return tuple.Success ? Success(tuple.Value) : Failure("Operation failed");
    }

    /// Defines an operator overload for implicit or explicit conversion.
    /// Operators allow transformations or casting between specific value types and the `Result<T>` type.
    /// The implementation determines how a particular tuple or value is translated into a success or failure `Result<T>`.
    /// For specific tuple-based inputs, this operator provides semantic processing, such as mapping to success
    /// based on provided values or marking as failure when error details are included.
    /// Supported tuple or type conversions should respect internal checks (such as null/empty constraints
    /// for error messages) and provide meaningful error feedback when such checks fail.
    /// Example:
    /// - A `ValueTuple` with a success indicator, an error message, and value might convert directly to a `Result<T>`.
    /// - A single value might be directly treated as success unless explicitly processed otherwise in the implementation.
    /// For error/debugging scenarios, exceptions within conversions should encapsulate relevant details like bad values.
    public static implicit operator Result<T>((bool Success, string Error, T Value) tuple)
    {
        return tuple.Success ? Success(tuple.Value) : Failure(tuple.Error);
    }

    /// Defines an operator for casting a tuple with three elements (Success, Exception, Value) into a Result<T>.
    /// This operator is implicit, meaning the conversion happens automatically without the need for explicit casting.
    /// The tuple comprises:
    /// - A boolean value indicating success.
    /// - An Exception object that represents the error (if any).
    /// - A value of type T that is returned if the operation is successful.
    /// The conversion logic is as follows:
    /// - If Success is true, the conversion returns a Result<T> in a successful state, using the Value.
    /// - If Success is false, the conversion returns a Result<T> in a failure state, using the Exception.
    /// - The Value property is not accessed if Success is false.
    /// Throws:
    /// - InvalidOperationException if an invalid tuple configuration occurs during processing.
    /// This operator facilitates seamless interaction between tuple patterns and the Result<T> structure.
    public static implicit operator Result<T>((bool Success, Exception Exception, T Value) tuple)
    {
        return tuple.Success ? Success(tuple.Value) : Failure(tuple.Exception);
    }

    // Explicit conversion to T (may throw)
    /// Defines an explicit conversion operator for converting a Result<T> instance to its underlying value of type T.
    /// This conversion will throw an exception if the result is in a failure state.
    /// Throws:
    /// InvalidOperationException: Thrown when the result is in a failure state and an attempt is made to access the value.
    public static explicit operator T(Result<T> result)
    {
        return result.Value;
    }

    // Value access methods
    /// Attempts to retrieve the value stored in the result if it represents a success.
    /// <param name="value">
    /// When this method returns, contains the value of the result if it represents a success, or the default value of the type if it represents a failure.
    /// </param>
    /// <returns>
    /// True if the result represents a success and contains a value; otherwise, false.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue([NotNullWhen(true)] [MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return IsSuccess;
    }

    /// Tries to retrieve the error message if the result is in a failure state.
    /// <param name="error">When this method returns, contains the error message if the result is in a failure state; otherwise, null.</param>
    /// <returns>True if the result is in a failure state, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetError([MaybeNullWhen(false)] out string error)
    {
        error = Error;
        return IsFailure;
    }

    /// <summary>
    /// Attempts to retrieve the exception associated with a failed result.
    /// </summary>
    /// <param name="exception">
    /// When the method returns true, this parameter contains the exception associated with the failure.
    /// When the method returns false, this parameter is set to null.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the result is in a failure state and has an associated exception.
    /// Returns true if the result is in a failure state and contains a non-null exception.
    /// Returns false otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetException([NotNullWhen(true)] out Exception? exception)
    {
        exception = Exception;
        return IsFailure && Exception != null;
    }

    /// Attempts to retrieve the value from the current Result if it is in the success state.
    /// If the Result is in the failure state, it returns false and provides the associated error message
    /// or exception, if available.
    /// <param name="value">When this method returns, contains the value of the Result if it is in the success state, or the default value of T if the Result is in the failure state.</param>
    /// <param name="error">When this method returns, contains the error message if the Result is in the failure state, or null if the Result is in the success state.</param>
    /// <param name="exception">When this method returns, contains the exception if the Result is in the failure state and has an associated exception, or null otherwise.</param>
    /// <returns>True if the Result is in the success state; otherwise, false.</returns>
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
    /// Retrieves the value if the result is successful; otherwise, returns the specified default value.
    /// <param name="defaultValue">The value to return if the result is in a failure state.</param>
    /// <returns>The result value if the operation is successful; otherwise, the specified default value.
    public T GetValueOrDefault(T defaultValue)
    {
        return IsSuccess ? Value : defaultValue;
    }

    /// Returns the value if the Result is in a success state; otherwise, returns the provided default value.
    /// <param name="defaultValue">
    /// The default value to return if the Result is not in a success state.</param>
    /// <returns>
    /// The value from the successful Result or the provided default value if the Result is not successful.
    /// </returns>
    public T GetValueOrDefault(Func<T> defaultValueFactory)
    {
        return IsSuccess ? Value : defaultValueFactory();
    }

    // Functional methods
    /// Transforms the value of a successful result using the given mapper function.
    /// If the result is a failure, the failure state is propagated without invoking the mapper.
    /// <typeparam name="TResult">The type of the value after mapping.</typeparam>
    /// <param name="mapper">A function that transforms the value of the result if it is successful.</param>
    /// <returns>
    /// A new result of type <typeparamref name="TResult"/> where the value is the result of the mapping transformation if successful,
    /// or the same failure state otherwise.
    /// </returns>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return IsSuccess ? Result<TResult>.Success(mapper(Value)) :
            Exception != null ? Result<TResult>.Failure(Exception) : Result<TResult>.Failure(Error);
    }

    /// Asynchronously transforms the result's value using the specified asynchronous mapping function.
    /// If the result is a success, the mapping function is applied to the value, and a new success result is created with the mapped value.
    /// If the result is a failure, the original failure state is preserved.
    /// <param name="mapper">The asynchronous function to transform the result's value if the result is successful.</param>
    /// <typeparam name="TResult">The type of the output result's value after mapping.</typeparam>
    /// <returns>A new result of type <see cref="Result{TResult}"/> representing the transformed value if successful, or the original failure if not.</returns>
    public async Task<Result<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> mapper)
    {
        return IsSuccess ? Result<TResult>.Success(await mapper(Value)) :
            Exception != null ? Result<TResult>.Failure(Exception) : Result<TResult>.Failure(Error);
    }

    /// Maps the error message of a failed result to a new error message using the provided mapping function.
    /// <param name="errorMapper">A function to transform the current error message into a new error message. This function is only applied if the result represents a failure.</param>
    /// <returns>A new result with the error message mapped if the current result is a failure, or the original result if it represents success.</returns>
    public Result<T> MapError(Func<string, string> errorMapper)
    {
        return IsSuccess ? this : Failure(errorMapper(Error));
    }

    /// Maps the current exception of the result to a new exception using the provided mapping function.
    /// <param name="exceptionMapper">
    /// A function to map the current exception to a new exception.
    /// The function takes the current exception (or null if none) as input and returns a mapped exception.
    /// </param>
    /// <returns>
    /// A new result containing the mapped exception if the current result is in a failure state;
    /// otherwise, the current result remains unchanged.
    /// </returns>
    public Result<T> MapException(Func<Exception?, Exception> exceptionMapper)
    {
        return IsSuccess ? this : Failure(exceptionMapper(Exception));
    }

    /// Binds the result of the current operation to a new result by applying the provided function.
    /// If the current result is successful, the binder function is executed with the value of the current result.
    /// If the current result is a failure, the failure is propagated to the resulting operation without invoking the binder.
    /// <param name="binder">A function that accepts the successful value of the current result and returns a new result.</param>
    /// <typeparam name="TResult">The type of the value in the resulting operation.</typeparam>
    /// <returns>
    /// A new result of type <see cref="Result{TResult}"/>. If the current result is a success, the value is passed to the binder function.
    /// If the current result is a failure, the resulting failure is returned without invoking the binder.
    /// </returns>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
    {
        return IsSuccess ? binder(Value) :
            Exception != null ? Result<TResult>.Failure(Exception) : Result<TResult>.Failure(Error);
    }

    /// Binds the result of the current operation to another asynchronous operation, transferring
    /// success state and maintaining error or exception in case of failure.
    /// <param name="binder">A function that takes the current successful value and returns a task
    /// representing the next result.</param>
    /// <typeparam name="TResult">The type of the result produced by the binder function.</typeparam>
    /// <returns>A task resulting in a new result of type <typeparamref name="TResult"/>, maintaining
    /// success or propagating original failure if the current result is unsuccessful or has an exception.</returns>
    public async Task<Result<TResult>> BindAsync<TResult>(Func<T, Task<Result<TResult>>> binder)
    {
        return IsSuccess ? await binder(Value) :
            Exception != null ? Result<TResult>.Failure(Exception) : Result<TResult>.Failure(Error);
    }

    /// Converts the current generic Result instance to a non-generic Result instance.
    /// If the current Result contains a successful state, the resulting non-generic Result will represent a success.
    /// If the current Result contains a failed state, the resulting non-generic Result will represent a failure, carrying either the associated error message or exception.
    /// <returns>A non-generic Result instance representing the same state as the current generic Result.</returns>
    public Result ToResult()
    {
        return IsSuccess ? Result.Success() :
            Exception != null ? Result.Failure(Exception) : Result.Failure(Error);
    }

    /// Executes a specified action if the result is successful and returns the current result instance.
    /// <param name="action">The action to execute if the result is successful. Receives the value of the result as a parameter.</param>
    /// <returns>The same result instance, allowing for method chaining.</returns>
    public Result<T> Tap(Action<T> action)
    {
        if (IsSuccess) action(Value);
        return this;
    }

    /// Executes an asynchronous action if the current Result is in a success state.
    /// This method ensures the result remains unchanged after the execution of the action.
    /// <param name="action">An asynchronous action to be executed if the result is successful. The action receives the value of the result as its parameter.</param>
    /// <returns>A Task containing the same Result instance, unchanged even after the action executes.</returns>
    public async Task<Result<T>> TapAsync(Func<T, Task> action)
    {
        if (IsSuccess) await action(Value);
        return this;
    }

    /// <summary>
    /// Ensures that the result satisfies the specified predicate. If the predicate fails, it transitions the result into a failure state with the provided error message.
    /// </summary>
    /// <param name="predicate">The predicate function to validate against the result's value.</param>
    /// <param name="error">The error message to set if the predicate validation fails.</param>
    /// <returns>A new result instance. If the predicate passes, the original result is returned. Otherwise, a failure result is created with the specified error message.</returns>
    public Result<T> Ensure(Func<T, bool> predicate, string error)
    {
        return IsFailure ? this : predicate(Value) ? this : Failure(error);
    }

    /// Ensures the current result meets a specified condition. If the result is in a failure state, it is returned unchanged. If it is in a success state, the specified predicate is evaluated. If the predicate returns false, a failure result with the provided exception is returned.
    /// <param name="predicate">A function that evaluates a condition on the result's value. Returns true if the condition is met, otherwise false.</param>
    /// <param name="exception">The exception to use for the failure result if the predicate condition is not met.</param>
    /// <returns>A success result if the current result is already in a success state and the predicate condition is met; otherwise, a failure result.</returns>
    public Result<T> Ensure(Func<T, bool> predicate, Exception exception)
    {
        return IsFailure ? this : predicate(Value) ? this : Failure(exception);
    }

    /// Ensures that the result is successful and the provided asynchronous predicate is satisfied.
    /// If the predicate is not satisfied, converts the result to a failure state with the specified error message.
    /// <param name="predicate">An asynchronous function that takes the value of the result and returns a task representing the evaluation as a boolean.</param>
    /// <param name="error">A string representing the error message to use if the predicate is not satisfied.</param>
    /// <returns>A task representing the result of the operation. If the result is successful and the predicate is satisfied, returns the original result. Otherwise, returns a failure result with the provided error message.
    public async Task<Result<T>> EnsureAsync(Func<T, Task<bool>> predicate, string error)
    {
        if (IsFailure) return this;
        return await predicate(Value) ? this : Failure(error);
    }

    /// <summary>
    /// Ensures that the result satisfies a specified asynchronous predicate. If the predicate evaluates to false,
    /// the result is transformed into a failure with the provided exception.
    /// </summary>
    /// <param name="predicate">An asynchronous function that evaluates a condition on the result's value.</param>
    /// <param name="exception">The exception to be returned if the predicate evaluates to false.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> which retains its success state if the predicate passes,
    /// or a failed state with the specified exception if the predicate fails.
    /// </returns>
    public async Task<Result<T>> EnsureAsync(Func<T, Task<bool>> predicate, Exception exception)
    {
        if (IsFailure) return this;
        return await predicate(Value) ? this : Failure(exception);
    }

    // Pattern matching with exception support
    /// Evaluates the result and invokes the appropriate action based on whether the Result is in a success or failure state.
    /// <param name="onSuccess">The action to perform when the result is successful. The result value is provided as a parameter to this action.</param>
    /// <param name="onFailure">The action to perform when the result is a failure. The error message and associated exception (if any) are provided as parameters to this action.</param>
    /// <returns>The current result instance to enable chaining of operations or further evaluations.
    public Result<T> Match(Action<T> onSuccess, Action<string, Exception?> onFailure)
    {
        if (IsSuccess) onSuccess(Value);
        else onFailure(Error, Exception);
        return this;
    }

    /// <summary>
    /// Evaluates a <see cref="Result{T}"/> based on its state and executes the appropriate callback.
    /// </summary>
    /// <param name="onSuccess">
    /// A function to execute when the result is successful, receiving the value of the result as a parameter.
    /// </param>
    /// <param name="onFailure">
    /// A function to execute when the result is unsuccessful, receiving the error message and optional exception as parameters.
    /// </param>
    /// <typeparam name="TResult">
    /// The type of the return value yielded after matching the result.
    /// </typeparam>
    /// <returns>
    /// The value returned by the executed callback function.
    /// </returns>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, Exception?, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error, Exception);
    }

    /// <summary>
    /// Executes the specified asynchronous actions based on the result's state.
    /// Invokes <paramref name="onSuccess"/> if the result indicates success,
    /// or <paramref name="onFailure"/> if the result indicates failure.
    /// </summary>
    /// <param name="onSuccess">
    /// A function to execute when the result is successful. Takes the successful value as an argument.
    /// </param>
    /// <param name="onFailure">
    /// A function to execute when the result is a failure. Takes the error message and the optional exception as arguments.
    /// </param>
    /// <returns>
    /// Returns the same <see cref="Result{T}"/> instance after asynchronously executing the corresponding actions.
    /// </returns>
    public async Task<Result<T>> MatchAsync(Func<T, Task> onSuccess, Func<string, Exception?, Task> onFailure)
    {
        if (IsSuccess) await onSuccess(Value);
        else await onFailure(Error, Exception);
        return this;
    }

    /// Executes the provided asynchronous functions based on the success or failure state of the result.
    /// The appropriate function is invoked depending on whether the result represents a success or failure.
    /// <param name="onSuccess">
    /// A function to execute if the result is successful. The input parameter is the result value.
    /// Returns a task of type <typeparamref name="TResult"/>.
    public async Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<string, Exception?, Task<TResult>> onFailure)
    {
        return IsSuccess ? await onSuccess(Value) : await onFailure(Error, Exception);
    }

    // Pattern matching without exception for backward compatibility
    /// Evaluates the result instance by branching based on its success or failure state,
    /// executing the appropriate action or function depending on the result.
    /// <param name="onSuccess">The action or function to invoke if the result is successful, receiving the result value as input.</param>
    /// <param name="onFailure">The action or function to invoke if the result is a failure, receiving the error message and optional exception as input.</param>
    /// <returns>If the result is successful, the value returned from the success function is returned.
    /// Otherwise, the value returned from the failure function is returned.</returns>
    public Result<T> Match(Action<T> onSuccess, Action<string> onFailure)
    {
        if (IsSuccess) onSuccess(Value);
        else onFailure(Error);
        return this;
    }

    /// Executes one of two actions based on the success or failure state of the result.
    /// <param name="onSuccess">
    /// The action to execute if the result is in a success state.
    /// This action receives the result value as its parameter.
    /// </param>
    /// <param name="onFailure">
    /// The action to execute if the result is in a failure state.
    /// This action receives the error message as its parameter.
    /// </param>
    /// <returns>
    /// The same result instance, allowing method chaining.
    /// </returns>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}