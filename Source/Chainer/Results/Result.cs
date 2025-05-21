using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Chainer.Results;

/// <summary>
/// Represents the result of an operation, encapsulating success or failure.
/// This struct provides mechanisms to create, manipulate, and evaluate the outcome of operations
/// by encapsulating result states, error messages, and optional exceptions.
/// </summary>
public readonly struct Result : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> struct, encapsulating whether an operation was successful or failed,
    /// along with associated error information or exception details.
    /// </summary>
    /// <param name="isSuccess">A boolean value indicating whether the operation was successful.</param>
    /// <param name="error">The error message if the operation failed; otherwise, <see cref="string.Empty"/>.</param>
    /// <param name="exception">The exception associated with the failure, if any; otherwise, null.</param>
    private Result(bool isSuccess, string error, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Error = isSuccess ? string.Empty : error;
        Exception = isSuccess ? null : exception;
    }

    /// <summary>
    /// Gets a value indicating whether the operation represented by the result was successful.
    /// </summary>
    /// <value><c>true</c> if the operation was successful; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// This property is typically used to check the outcome of an operation encapsulated by a result type.
    /// </remarks>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation has failed.
    /// </summary>
    /// <value><c>true</c> if the operation is not successful; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// This property is a complement to <see cref="IsSuccess"/> and is derived
    /// by negating its value. If <see cref="IsSuccess"/> is true, this property
    /// will return false, and if <see cref="IsSuccess"/> is false, this property
    /// will return true.
    /// </remarks>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message associated with a failed result.
    /// </summary>
    /// <value>The error message if the result is a failure; otherwise, <see cref="string.Empty"/>.</value>
    /// <remarks>
    /// This property provides a human-readable message describing the reason for the failure.
    /// </remarks>
    public string Error { get; }

    /// <summary>
    /// Gets the exception associated with the failure of an operation, if any.
    /// </summary>
    /// <value>The <see cref="System.Exception"/> associated with the failure, or <c>null</c> if the operation was successful or no exception was provided.</value>
    /// <remarks>
    /// Use this property to retrieve detailed information about the error that caused
    /// the operation to fail, especially in cases where an exception is available and
    /// provides additional context beyond the error message.
    /// </remarks>
    public Exception? Exception { get; }

    /// <summary>
    /// Returns a string representation of the current <see cref="Result"/> instance.
    /// </summary>
    /// <returns>
    /// "Success" if the result is successful; otherwise, "Failure" followed by the error message (e.g., "Failure(Error message)").
    /// </returns>
    public override string ToString()
    {
        return IsSuccess ? "Success" : $"Failure({Error})";
    }

    // Factory methods
    /// <summary>
    /// Creates a successful <see cref="Result"/> instance.
    /// </summary>
    /// <returns>A new instance of the <see cref="Result"/> struct indicating a successful operation.</returns>
    public static Result Success()
    {
        return new Result(true, string.Empty);
    }

    /// <summary>
    /// Creates a failed <see cref="Result"/> with the given error message.
    /// </summary>
    /// <param name="error">The error message describing the failure. It cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A new instance of the <see cref="Result"/> struct, representing a failure with the specified error message.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided <paramref name="error"/> message is null or white space.</exception>
    public static Result Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty", nameof(error));

        return new Result(false, error);
    }

    /// <summary>
    /// Creates a failed <see cref="Result"/> from the specified exception.
    /// The exception's message is used as the error message for the result.
    /// </summary>
    /// <param name="exception">The exception that caused the failure. Cannot be null.</param>
    /// <returns>A failed <see cref="Result"/> instance containing the exception's message and the exception itself.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="exception"/> is null.</exception>
    public static Result Failure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new Result(false, exception.Message, exception);
    }

    // Conversions
    /// <summary>
    /// Defines an implicit conversion from a <see cref="bool"/> to a <see cref="Result"/>.
    /// <c>true</c> converts to a successful <see cref="Result"/>;
    /// <c>false</c> converts to a failed <see cref="Result"/> with a default error message "Operation failed".
    /// </summary>
    /// <param name="success">A boolean value indicating success (<c>true</c>) or failure (<c>false</c>).</param>
    /// <returns>A <see cref="Result"/> object representing either success or failure based on the input <paramref name="success"/> value.</returns>
    public static implicit operator Result(bool success)
    {
        return success ? Success() : Failure("Operation failed");
    }

    /// <summary>
    /// Defines an implicit conversion from an <see cref="Exception"/> to a <see cref="Result"/>.
    /// This conversion creates a failure <see cref="Result"/> instance, encapsulating the provided exception.
    /// The exception's message will be used as the <see cref="Error"/> property of the <see cref="Result"/>.
    /// </summary>
    /// <param name="exception">The exception to convert. Must not be null.</param>
    /// <returns>A failure <see cref="Result"/> instance containing the error message and <see cref="System.Exception"/> derived from the provided <paramref name="exception"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="exception"/> is null.</exception>
    public static implicit operator Result(Exception exception)
    {
        return Failure(exception);
    }

    // Core methods that work with non-generic Result
    /// <summary>
    /// Ensures that the current result satisfies the specified predicate if the result is currently successful.
    /// If the current result is already a failure, it's returned as is.
    /// If the predicate is not satisfied, a new failure <see cref="Result"/> is returned with the provided error message.
    /// </summary>
    /// <param name="predicate">The function to evaluate. It must return <c>true</c> for the condition to be satisfied.</param>
    /// <param name="error">The error message to use if the predicate returns <c>false</c>.</param>
    /// <returns>
    /// The original <see cref="Result"/> if it's already a failure or if it's a success and the predicate returns <c>true</c>.
    /// Otherwise, a new failure <see cref="Result"/> with the specified <paramref name="error"/>.
    /// </returns>
    public Result Ensure(Func<bool> predicate, string error)
    {
        if (IsFailure) return this;
        return predicate() ? this : Failure(error);
    }

    /// <summary>
    /// Ensures that the current result satisfies the specified predicate if the result is currently successful.
    /// If the current result is already a failure, it's returned as is.
    /// If the predicate is not satisfied, a new failure <see cref="Result"/> is returned, created from the provided exception.
    /// </summary>
    /// <param name="predicate">The function to evaluate. It must return <c>true</c> for the condition to be satisfied.</param>
    /// <param name="exception">The exception to use for creating the failure <see cref="Result"/> if the predicate returns <c>false</c>.</param>
    /// <returns>
    /// The original <see cref="Result"/> if it's already a failure or if it's a success and the predicate returns <c>true</c>.
    /// Otherwise, a new failure <see cref="Result"/> created from the specified <paramref name="exception"/>.
    /// </returns>
    public Result Ensure(Func<bool> predicate, Exception exception)
    {
        if (IsFailure) return this;
        return predicate() ? this : Failure(exception);
    }

    /// <summary>
    /// Ensures that the current result satisfies an asynchronous predicate if the result is currently successful.
    /// If the current result is already a failure, it's returned as is.
    /// If the asynchronous predicate is not satisfied, a new failure <see cref="Result"/> is returned with the provided error message.
    /// </summary>
    /// <param name="predicate">An asynchronous function that returns a <see cref="Task{TResult}"/> of <see cref="bool"/>. It must return <c>true</c> for the condition to be satisfied.</param>
    /// <param name="error">The error message to use if the predicate returns <c>false</c>.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
    /// The task result is the original <see cref="Result"/> if it's already a failure or if it's a success and the predicate returns <c>true</c>.
    /// Otherwise, it's a new failure <see cref="Result"/> with the specified <paramref name="error"/>.
    /// </returns>
    public async Task<Result> EnsureAsync(Func<Task<bool>> predicate, string error)
    {
        if (IsFailure) return this;
        return await predicate().ConfigureAwait(false) ? this : Failure(error);
    }

    /// <summary>
    /// Ensures that the current result satisfies an asynchronous predicate if the result is currently successful.
    /// If the current result is already a failure, it's returned as is.
    /// If the asynchronous predicate is not satisfied, a new failure <see cref="Result"/> is returned, created from the provided exception.
    /// </summary>
    /// <param name="predicate">An asynchronous function that returns a <see cref="Task{TResult}"/> of <see cref="bool"/>. It must return <c>true</c> for the condition to be satisfied.</param>
    /// <param name="exception">The exception to use for creating the failure <see cref="Result"/> if the predicate returns <c>false</c>.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
    /// The task result is the original <see cref="Result"/> if it's already a failure or if it's a success and the predicate returns <c>true</c>.
    /// Otherwise, it's a new failure <see cref="Result"/> created from the specified <paramref name="exception"/>.
    /// </returns>
    public async Task<Result> EnsureAsync(Func<Task<bool>> predicate, Exception exception)
    {
        if (IsFailure) return this;
        return await predicate().ConfigureAwait(false) ? this : Failure(exception);
    }

    /// <summary>
    /// If the result is a failure, maps its <see cref="Error"/> message to a new error message using the specified mapping function.
    /// If the result is a success, it's returned unchanged.
    /// </summary>
    /// <param name="errorMapper">A function that takes the current error message and returns a new error message.</param>
    /// <returns>
    /// A new failure <see cref="Result"/> with the mapped error message if the current result is a failure;
    /// otherwise, the original success <see cref="Result"/>.
    /// </returns>
    public Result MapError(Func<string, string> errorMapper)
    {
        if (IsSuccess) return this;
        return Failure(errorMapper(Error));
    }

    /// <summary>
    /// If the result is a failure, maps its <see cref="Exception"/> (if any) to a new exception using the specified mapping function.
    /// The new failure <see cref="Result"/> will use the message of the mapped exception as its error message.
    /// If the result is a success, it's returned unchanged.
    /// </summary>
    /// <param name="exceptionMapper">A function that transforms the current exception (or null if none) into a new exception.</param>
    /// <returns>
    /// A new failure <see cref="Result"/> with the mapped exception if the current result is a failure;
    /// otherwise, the original successful <see cref="Result"/>.
    /// </returns>
    public Result MapException(Func<Exception?, Exception> exceptionMapper)
    {
        if (IsSuccess) return this;
        return Failure(exceptionMapper(Exception));
    }

    /// <summary>
    /// Executes the specified action if the current <see cref="Result"/> is successful.
    /// This method allows for "tapping into" the success path to perform side effects without altering the result.
    /// </summary>
    /// <param name="action">The action to execute if the <see cref="Result"/> is successful.</param>
    /// <returns>The original <see cref="Result"/> instance, allowing for fluent chaining.</returns>
    public Result Tap(Action action)
    {
        if (IsSuccess) action();
        return this;
    }

    /// <summary>
    /// Executes the specified asynchronous action if the current <see cref="Result"/> is successful.
    /// This method allows for "tapping into" the success path to perform asynchronous side effects without altering the result.
    /// </summary>
    /// <param name="action">The asynchronous action (returning a <see cref="Task"/>) to execute if the <see cref="Result"/> is successful.</param>
    /// <returns>A <see cref="Task{Result}"/> representing the asynchronous operation, which yields the original <see cref="Result"/> instance, allowing for fluent chaining.</returns>
    public async Task<Result> TapAsync(Func<Task> action)
    {
        if (IsSuccess) await action().ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// If the current <see cref="Result"/> is successful, executes the <paramref name="binder"/> function and returns its result.
    /// If the current <see cref="Result"/> is a failure, it returns the current failure <see cref="Result"/> without executing the function.
    /// This is used for chaining operations that return a <see cref="Result"/>.
    /// </summary>
    /// <param name="binder">A function that takes no arguments and returns a <see cref="Result"/>. This function is executed only if the current result is successful.</param>
    /// <returns>The <see cref="Result"/> from the <paramref name="binder"/> function if the current result is successful; otherwise, the current failure <see cref="Result"/>.</returns>
    public Result Bind(Func<Result> binder)
    {
        return IsSuccess ? binder() : this;
    }

    /// <summary>
    /// If the current <see cref="Result"/> is successful, executes the asynchronous <paramref name="binder"/> function and returns its result.
    /// If the current <see cref="Result"/> is a failure, it returns a <see cref="Task{Result}"/> containing the current failure <see cref="Result"/> without executing the function.
    /// This is used for chaining asynchronous operations that return a <see cref="Result"/>.
    /// </summary>
    /// <param name="binder">An asynchronous function that takes no arguments and returns a <see cref="Task{Result}"/>. This function is executed only if the current result is successful.</param>
    /// <returns>
    /// A <see cref="Task{Result}"/> representing the asynchronous operation.
    /// The task will yield the <see cref="Result"/> from the <paramref name="binder"/> function if the current result was successful;
    /// otherwise, it will yield the current failure <see cref="Result"/>.
    /// </returns>
    public async Task<Result> BindAsync(Func<Task<Result>> binder)
    {
        return IsSuccess ? await binder().ConfigureAwait(false) : this;
    }

    // Pattern matching
    /// <summary>
    /// Executes one of the provided actions based on whether the <see cref="Result"/> is a success or a failure.
    /// </summary>
    /// <param name="onSuccess">The action to execute if the <see cref="Result"/> is successful.</param>
    /// <param name="onFailure">The action to execute if the <see cref="Result"/> is a failure. It receives the error message and the (optional) exception.</param>
    /// <returns>The original <see cref="Result"/> instance, allowing for method chaining if needed.</returns>
    public Result Match(Action onSuccess, Action<string, Exception?> onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure(Error, Exception);
        return this;
    }

    /// <summary>
    /// Executes one of the provided functions based on whether the <see cref="Result"/> is a success or a failure, and returns its result.
    /// </summary>
    /// <typeparam name="TResult">The type of the value to be returned by the match functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the <see cref="Result"/> is successful. Returns a <typeparamref name="TResult"/>.</param>
    /// <param name="onFailure">The function to execute if the <see cref="Result"/> is a failure. It receives the error message and the (optional) exception, and returns a <typeparamref name="TResult"/>.</param>
    /// <returns>The value returned by either the <paramref name="onSuccess"/> or <paramref name="onFailure"/> function.</returns>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, Exception?, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error, Exception);
    }

    /// <summary>
    /// Asynchronously executes one of the provided asynchronous actions based on whether the <see cref="Result"/> is a success or a failure.
    /// </summary>
    /// <param name="onSuccess">The asynchronous action (returning a <see cref="Task"/>) to execute if the <see cref="Result"/> is successful.</param>
    /// <param name="onFailure">The asynchronous action (returning a <see cref="Task"/>) to execute if the <see cref="Result"/> is a failure. It receives the error message and the (optional) exception.</param>
    /// <returns>A <see cref="Task{Result}"/> representing the asynchronous match operation, which yields the original <see cref="Result"/> instance.</returns>
    public async Task<Result> MatchAsync(Func<Task> onSuccess, Func<string, Exception?, Task> onFailure)
    {
        if (IsSuccess) await onSuccess().ConfigureAwait(false);
        else await onFailure(Error, Exception).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Asynchronously executes one of the provided asynchronous functions based on whether the <see cref="Result"/> is a success or a failure, and returns its result.
    /// </summary>
    /// <typeparam name="TResult">The type of the value to be returned by the match functions.</typeparam>
    /// <param name="onSuccess">The asynchronous function (returning a <see cref="Task{TResult}"/> of <typeparamref name="TResult"/>) to execute if the <see cref="Result"/> is successful.</param>
    /// <param name="onFailure">The asynchronous function (returning a <see cref="Task{TResult}"/> of <typeparamref name="TResult"/>) to execute if the <see cref="Result"/> is a failure. It receives the error message and the (optional) exception.</param>
    /// <returns>A <see cref="Task{TResult}"/> that will yield the value returned by either the <paramref name="onSuccess"/> or <paramref name="onFailure"/> asynchronous function.</returns>
    public async Task<TResult> MatchAsync<TResult>(Func<Task<TResult>> onSuccess, Func<string, Exception?, Task<TResult>> onFailure)
    {
        return IsSuccess ? await onSuccess().ConfigureAwait(false) : await onFailure(Error, Exception).ConfigureAwait(false);
    }

    // Simplified Match overloads (without Exception parameter for onFailure)
    /// <summary>
    /// Executes one of the provided actions based on whether the <see cref="Result"/> is a success or a failure.
    /// This overload of Match provides an <paramref name="onFailure"/> action that only accepts the error message.
    /// </summary>
    /// <param name="onSuccess">An action to execute if the result represents a success.</param>
    /// <param name="onFailure">An action to execute if the result represents a failure, provided with the error message.</param>
    /// <returns>The original <see cref="Result"/> instance, allowing for chaining further operations.</returns>
    public Result Match(Action onSuccess, Action<string> onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure(Error);
        return this;
    }

    /// <summary>
    /// Executes one of the provided functions based on whether the <see cref="Result"/> is a success or a failure, and returns its result.
    /// This overload of Match provides an <paramref name="onFailure"/> function that only accepts the error message.
    /// </summary>
    /// <typeparam name="TResult">The type of the value to be returned by the match functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the <see cref="Result"/> is successful. Returns a <typeparamref name="TResult"/>.</param>
    /// <param name="onFailure">The function to execute if the <see cref="Result"/> is a failure. It receives the error message and returns a <typeparamref name="TResult"/>.</param>
    /// <returns>The value returned by either the <paramref name="onSuccess"/> or <paramref name="onFailure"/> function.</returns>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error);
    }

    /// <summary>
    /// Tries to get the error message if the <see cref="Result"/> is a failure.
    /// </summary>
    /// <param name="error">
    /// When this method returns, contains the error message if the operation failed; otherwise, <c>null</c>.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if the <see cref="Result"/> is a failure (i.e., <see cref="IsFailure"/> is <c>true</c>); otherwise, <c>false</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetError([MaybeNullWhen(false)] out string error)
    {
        error = Error; // Will be string.Empty if IsSuccess is true
        return IsFailure;
    }

    /// <summary>
    /// Tries to get the <see cref="System.Exception"/> if the <see cref="Result"/> is a failure and an exception is present.
    /// </summary>
    /// <param name="exception">
    /// When this method returns, contains the <see cref="System.Exception"/> associated with the failed result, if available; otherwise <c>null</c>.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if the <see cref="Result"/> represents a failure and an <see cref="System.Exception"/> is available; otherwise, <c>false</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetException([NotNullWhen(true)] out Exception? exception)
    {
        exception = Exception;
        return IsFailure && Exception != null;
    }
}