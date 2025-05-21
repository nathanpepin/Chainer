namespace Chainer.Results;

/// Represents an operation result, providing information about success or failure,
/// an optional error message, and an optional exception.
public interface IResult
{
    /// Gets a value indicating whether the operation represented by this result
    /// was successful.
    /// A return value of true indicates that the result represents a successful
    /// outcome, and the associated error or exception properties should not be
    /// accessed. Conversely, a false value indicates a failure, and more details
    /// can be retrieved via the Error or Exception properties if available.
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation result represents a failure.
    /// </summary>
    /// <remarks>
    /// This property is the logical negation of <see cref="IsSuccess"/>. It returns true if the result
    /// indicates a failure, and false if the result indicates success.
    /// </remarks>
    bool IsFailure { get; }

    /// Gets the error message associated with a failure result.
    /// This property provides a description of the failure when the result
    /// represents an error state. It can be used to understand why a specific
    /// operation has failed. For a successful result, the value of this property
    /// is undefined and should not be accessed.
    /// In combination with the `IsFailure` property, the `Error` property allows
    /// for detailed inspection and handling of failure cases.
    string Error { get; }

    /// <summary>
    /// Gets the exception associated with the result, if any.
    /// </summary>
    /// <remarks>
    /// This property holds the exception that describes the error when the result is in a failure state
    /// and an exception is the cause of the failure. It will be null if the result is successful or
    /// if the failure is represented only by an error message without an associated exception.
    /// </remarks>
    /// <value>
    /// An <see cref="System.Exception"/> representing the error, or null if no exception exists.
    /// </value>
    Exception? Exception { get; }
}

/// <summary>
/// Represents the result of an operation, providing information about
/// success or failure along with associated metadata.
/// </summary>
public interface IResult<out T> : IResult
{
    /// Gets the value of the result if the result is in a successful state.
    /// Accessing this property when the result is in a failure state will throw an InvalidOperationException.
    /// Use this property to retrieve the encapsulated value of a successful result.
    /// If the result is in a failure state, the `Error` or `Exception` property provides details about the failure.
    T Value { get; }
}