using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Chainer.Results;

/// Represents the result of an operation, encapsulating success or failure.
/// This struct provides mechanisms to create, manipulate, and evaluate the outcome of operations
/// by encapsulating result states, error messages, and optional exceptions.
public readonly struct Result : IResult
{
    /// Represents the result of an operation, encapsulating whether the operation was successful or failed,
    /// along with associated error information or exception details.
    private Result(bool isSuccess, string error, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Error = isSuccess ? string.Empty : error;
        Exception = isSuccess ? null : exception;
    }

    /// Gets a value indicating whether the operation represented by the result was successful.
    /// Returns:
    /// True if the operation was successful; otherwise, false.
    /// This property is typically used to check the outcome of an operation encapsulated by a result type.
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation has failed.
    /// Returns true if the operation is not successful; otherwise, false.
    /// </summary>
    /// <remarks>
    /// This property is a complement to <see cref="IsSuccess"/> and is derived
    /// by negating its value. If <see cref="IsSuccess"/> is true, this property
    /// will return false, and if <see cref="IsSuccess"/> is false, this property
    /// will return true.
    /// </remarks>
    public bool IsFailure => !IsSuccess;

    /// Gets the error message associated with a failed result.
    /// This property is an empty string if the result is successful.
    /// It provides a human-readable message describing the reason for the failure.
    public string Error { get; }

    /// <summary>
    /// Gets the exception associated with the failure of an operation, if any.
    /// This property is null if the operation was successful or if no exception
    /// is associated with the failure.
    /// </summary>
    /// <remarks>
    /// Use this property to retrieve detailed information about the error that caused
    /// the operation to fail, especially in cases where an exception is available and
    /// provides additional context beyond the error message.
    /// </remarks>
    public Exception? Exception { get; }

    /// Returns a string representation of the current Result instance.
    /// The returned string will indicate the success or failure state of the Result.
    /// If the result is successful, it returns "Success".
    /// If the result is a failure, it returns "Failure" followed by the error message.
    /// <returns>
    /// A string indicating the success or failure status of the Result.
    /// </returns>
    public override string ToString()
    {
        return IsSuccess ? "Success" : $"Failure({Error})";
    }

    // Factory methods
    /// Returns a successful result instance.
    /// <returns>A new instance of the <see cref="Result"/> struct indicating a successful operation.</returns>
    public static Result Success()
    {
        return new Result(true, string.Empty);
    }

    /// Creates a failed result with the given error message.
    /// <param name="error">The error message describing the failure. It cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A new instance of the <see cref="Result"/> struct, representing a failure with the specified error message.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided error message is null or white space.</exception>
    public static Result Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty", nameof(error));

        return new Result(false, error);
    }

    /// <summary>
    /// Creates a failed <see cref="Result"/> with a specified error message.
    /// </summary>
    /// <param name="error">The error message describing the failure. Cannot be null or empty.</param>
    /// <returns>A failed <see cref="Result"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the provided error message is null or empty.</exception>
    public static Result Failure(Exception exception)
    {
        return new Result(false, exception.Message, exception);
    }

    // Conversions
    /// <summary>
    /// Defines an implicit conversion operator for the <see cref="Result"/> struct which converts a boolean value
    /// into a <see cref="Result"/> object. When the input is <c>true</c>, a successful <see cref="Result"/> is created.
    /// When the input is <c>false</c>, a failure <see cref="Result"/> is created with a default error message.
    /// </summary>
    /// <param name="success">A boolean value indicating success or failure.</param>
    /// <returns>A <see cref="Result"/> object where <paramref name="success"/> determines its state.</returns>
    public static implicit operator Result(bool success)
    {
        return success ? Success() : Failure("Operation failed");
    }

    /// Provides an implicit conversion operator for converting an exception into a Result structure.
    /// This conversion creates a failure Result instance, encapsulating the provided exception.
    /// Parameters:
    /// exception:
    /// The exception to be converted into a Result instance. Must not be null.
    /// Returns:
    /// A failure Result instance containing the error message and Exception, derived
    /// from the provided exception.
    /// Exceptions:
    /// ArgumentNullException:
    /// Thrown if the provided exception is null.
    public static implicit operator Result(Exception exception)
    {
        return Failure(exception);
    }

    // Core methods that work with non-generic Result
    /// <summary>
    /// Ensures that the current result satisfies the specified predicate.
    /// If the predicate is not satisfied, a failure result is returned with the provided error message.
    /// </summary>
    /// <param name="predicate">The function that verifies a condition on the current result.</param>
    /// <param name="error">The error message returned if the predicate fails.</param>
    /// <returns>
    /// A <see cref="Result"/> representing the current state. If the predicate is satisfied, the current result is returned,
    /// otherwise, a failure <see cref="Result"/> is returned with the specified error message.
    /// </returns>
    public Result Ensure(Func<bool> predicate, string error)
    {
        if (IsFailure) return this;
        return predicate() ? this : Failure(error);
    }

    /// <summary>
    /// Ensures that the current <see cref="Result"/> satisfies the given predicate.
    /// If the predicate evaluates to false, this method will return a failure <see cref="Result"/> with the specified error message.
    /// </summary>
    /// <param name="predicate">A function that evaluates a condition on the current <see cref="Result"/>.</param>
    /// <param name="error">The error message to associate with the result if the predicate returns false.</param>
    /// <returns>
    /// A success <see cref="Result"/> if the current result is successful and the predicate evaluates to true;
    /// otherwise, a failure <see cref="Result"/> with the specified error message.
    /// </returns>
    public Result Ensure(Func<bool> predicate, Exception exception)
    {
        if (IsFailure) return this;
        return predicate() ? this : Failure(exception);
    }

    /// Ensures the success state of a result based on an asynchronous condition.
    /// If the result is already in a failed state, it will remain unchanged.
    /// If the condition is not met, the result will transition to a failed state with the specified error message.
    /// <param name="predicate">An asynchronous function that represents the condition to evaluate.</param>
    /// <param name="error">The error message used if the condition is not met.</param>
    /// <returns>A new or unchanged result based on the condition evaluation.</returns>
    public async Task<Result> EnsureAsync(Func<Task<bool>> predicate, string error)
    {
        if (IsFailure) return this;
        return await predicate() ? this : Failure(error);
    }

    /// <summary>
    /// Ensures that a specified asynchronous predicate is satisfied; otherwise, sets the result as a failure with the provided exception.
    /// </summary>
    /// <param name="predicate">An asynchronous function that returns a boolean indicating whether the condition is satisfied.</param>
    /// <param name="exception">The exception to associate with the result in case the predicate fails.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure based on the evaluation of the predicate.</returns>
    public async Task<Result> EnsureAsync(Func<Task<bool>> predicate, Exception exception)
    {
        if (IsFailure) return this;
        return await predicate() ? this : Failure(exception);
    }

    /// <summary>
    /// Maps the current error message to a new error message using the specified mapping function.
    /// </summary>
    /// <param name="errorMapper">A function that takes the current error message and returns the mapped error message.</param>
    /// <returns>
    /// A new <see cref="Result"/> with the mapped error message if the current result represents a failure;
    /// otherwise, the original success <see cref="Result"/>.
    /// </returns>
    public Result MapError(Func<string, string> errorMapper)
    {
        if (IsSuccess) return this;
        return Failure(errorMapper(Error));
    }

    /// <summary>
    /// Maps the exception of a failed result using the specified exception mapper function.
    /// </summary>
    /// <param name="exceptionMapper">A function that transforms the current exception into a new exception.</param>
    /// <returns>
    /// A new <see cref="Result"/> instance with the mapped exception if the current result represents a failure;
    /// otherwise, the original successful result.
    /// </returns>
    public Result MapException(Func<Exception?, Exception> exceptionMapper)
    {
        if (IsSuccess) return this;
        return Failure(exceptionMapper(Exception));
    }

    /// Executes a specified action if the current result indicates success.
    /// This method allows chaining additional logic to be executed without requiring
    /// further processing of the result.
    /// <param name="action">The action to execute if the result indicates success.</param>
    /// <returns>
    /// The current result instance, allowing for continued method chaining. If the current
    /// result does not indicate success, the provided action is not executed.
    /// </returns>
    public Result Tap(Action action)
    {
        if (IsSuccess) action();
        return this;
    }

    /// <summary>
    /// Executes an asynchronous action if the current result represents a success.
    /// </summary>
    /// <param name="action">The asynchronous action to execute if the result is a success.</param>
    /// <returns>The current result instance, allowing for method chaining.</returns>
    public async Task<Result> TapAsync(Func<Task> action)
    {
        if (IsSuccess) await action();
        return this;
    }

    /// Executes the specified function only if the current result is successful.
    /// If the result represents a failure, it returns the current failed result without executing the function.
    /// <param name="binder">A function to execute if the current result is successful. It returns a new result to replace the current one.</param>
    /// <returns>A new result if the current result is successful, otherwise the current result if it represents a failure.
    public Result Bind(Func<Result> binder)
    {
        return IsSuccess ? binder() : this;
    }

    /// <summary>
    /// Executes a binding function asynchronously on a successful result, propagating the result of the binding function.
    /// If the current result represents a failure, it propagates the failure without invoking the binding function.
    /// </summary>
    /// <param name="binder">A function that returns a Task of Result to bind to if the current result is successful.</param>
    /// <returns>A Task wrapping the result of the binder function if the current result is successful; otherwise, the original failed result.</returns>
    public async Task<Result> BindAsync(Func<Task<Result>> binder)
    {
        return IsSuccess ? await binder() : this;
    }

    // Pattern matching
    /// Executes the provided match actions based on the state of the Result.
    /// If the Result represents success, the `onSuccess` action is executed.
    /// If the Result represents failure, the `onFailure` action is executed with the error message and the associated exception (if any).
    /// <param name="onSuccess">The action to execute if the Result is successful.</param>
    /// <param name="onFailure">The action to execute if the Result is a failure. It accepts the error message and an optional exception as parameters.</param>
    /// <returns>The same Result instance, allowing method chaining if needed.
    public Result Match(Action onSuccess, Action<string, Exception?> onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure(Error, Exception);
        return this;
    }

    /// Matches the result state and invokes the appropriate callback functions based on success or failure states.
    /// <param name="onSuccess">The action to invoke when the result signifies success.</param>
    /// <param name="onFailure">The action to invoke when the result signifies failure. It receives the error message and an optional exception.</param>
    /// <returns>Returns the current result instance.</>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, Exception?, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error, Exception);
    }

    /// Asynchronously matches the result of the operation by executing the corresponding function for success or failure.
    /// Calls the asynchronous `onSuccess` function if the result is successful, or the asynchronous `onFailure` function if it is a failure.
    /// Both actions return a task for asynchronous operations.
    /// <param name="onSuccess">The asynchronous function to execute if the operation was successful.</param>
    /// <param name="onFailure">The asynchronous function to execute if the operation failed. Takes the error message and the associated exception, if any.</param>
    /// <returns>A task that represents the asynchronous match operation.
    /// If invoked on a success result, it returns the task after executing the `onSuccess`. On failure, it calls and returns the task executed by `onFailure`.</returns>
    public async Task<Result> MatchAsync(Func<Task> onSuccess, Func<string, Exception?, Task> onFailure)
    {
        if (IsSuccess) await onSuccess();
        else await onFailure(Error, Exception);
        return this;
    }

    /// Performs asynchronous pattern matching for a result object. Executes the appropriate handler based on
    /// the success or failure state of the result.
    /// If the result is successful, the provided onSuccess function is executed. Otherwise, the onFailure
    /// function is executed with the error message and the associated exception (if any).
    /// <param name="onSuccess">A function to execute when the result is successful. This function returns a task of type TResult.</param>
    /// <param name="onFailure">A function to execute when the result is a failure. This function accepts a string (error message) and an exception, and returns a task of type TResult.</param>
    /// <typeparam name="TResult">The type of the return value of the asynchronous handlers.</typeparam>
    /// <returns>A task that represents the outcome of either the onSuccess or onFailure function, depending on the result state.</returns>
    public async Task<TResult> MatchAsync<TResult>(Func<Task<TResult>> onSuccess, Func<string, Exception?, Task<TResult>> onFailure)
    {
        return IsSuccess ? await onSuccess() : await onFailure(Error, Exception);
    }

    // Try methods - simpler for backward compatibility
    /// <summary>
    /// Matches the result by executing appropriate actions based on the success or failure state.
    /// </summary>
    /// <param name="onSuccess">An action to execute if the result represents a success.</param>
    /// <param name="onFailure">An action to execute if the result represents a failure, provided with the error message.</param>
    /// <returns>The same <see cref="Result"/> instance, allowing for chaining further operations.</returns>
    public Result Match(Action onSuccess, Action<string> onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure(Error);
        return this;
    }

    /// Matches the result based on its success or failure state and executes the provided callbacks.
    /// <param name="onSuccess">The action to execute if the result is successful.</param>
    /// <param name="onFailure">The action to execute if the result is a failure. Receives the error message and an optional exception.</param>
    /// <returns>The current instance of the Result.</param>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error);
    }

    /// Determines if there is an error present and outputs the error string if available.
    /// <param name="error">
    /// When this method returns, contains the error string if the operation failed; otherwise, contains null.
    /// </param>
    /// <returns>
    /// Returns true if there is an error (operation failed); otherwise, false (operation succeeded).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetError([MaybeNullWhen(false)] out string error)
    {
        error = Error;
        return IsFailure;
    }

    /// Tries to retrieve the exception associated with a failed result.
    /// <param name="exception">
    /// When this method returns, contains the exception associated with the failed result, if available; otherwise null.
    /// </param>
    /// <returns>
    /// True if the result represents a failure and an exception is available; otherwise, false.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetException([NotNullWhen(true)] out Exception? exception)
    {
        exception = Exception;
        return IsFailure && Exception != null;
    }
}