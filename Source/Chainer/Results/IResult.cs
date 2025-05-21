namespace Chainer.Results;

/// <summary>
///     Defines a common interface for results of operations that may succeed or fail,
///     providing access to success status and error information.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="IResult"/> interface is the foundation of Chainer's functional result pattern, 
///         a safer alternative to exception-based error handling. It represents the outcome of an 
///         operation, which can be either a success or a failure, and encapsulates any error 
///         information when a failure occurs.
///     </para>
///     <para>
///         This pattern offers several advantages over traditional exception handling:
///         <list type="bullet">
///             <item>Makes failures explicit in method signatures, improving code predictability</item>
///             <item>Avoids the performance overhead of throwing and catching exceptions</item>
///             <item>Provides a consistent pattern for handling both expected and unexpected errors</item>
///             <item>Enables functional composition of operations with built-in error handling</item>
///             <item>Simplifies error propagation in asynchronous code</item>
///         </list>
///     </para>
///     <para>
///         In the Chainer library, this interface is used extensively throughout the chain execution
///         system. Chain handlers return results to indicate success or failure, and the chain
///         executor propagates these results through the execution pipeline. A failure at any
///         point stops the chain execution and returns the failure information.
///     </para>
///     <para>
///         The interface includes properties for checking success/failure status and accessing
///         error information, as well as an optional Exception property for cases where the
///         failure was caused by an exception.
///     </para>
///     <para>
///         Implementations of this interface include <see cref="Result"/> for simple operations
///         without return values and <see cref="Result{T}"/> for operations that return a value
///         when successful.
///     </para>
///     <para>
///         This pattern is particularly valuable in chain handlers, where many types of failures
///         can occur (validation errors, business rule violations, external service failures, etc.),
///         and explicit handling of these failures is crucial for robust processing.
///     </para>
/// </remarks>
public interface IResult
{
    /// <summary>
    ///     Gets a value indicating whether the operation completed successfully.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property returns true when the operation represented by this result
    ///         completed without errors, and false otherwise.
    ///     </para>
    ///     <para>
    ///         This is typically used as the primary check when handling results:
    ///         <code>
    ///         var result = await SomeOperation();
    ///         if (result.IsSuccess)
    ///         {
    ///             // Handle success case
    ///         }
    ///         else
    ///         {
    ///             // Handle failure case
    ///         }
    ///         </code>
    ///     </para>
    ///     <para>
    ///         For results that contain a value (<see cref="IResult{T}"/>), the Value property
    ///         is guaranteed to be valid only when IsSuccess is true. Accessing Value when
    ///         IsSuccess is false may throw an exception, depending on the implementation.
    ///     </para>
    /// </remarks>
    bool IsSuccess { get; }

    /// <summary>
    ///     Gets a value indicating whether the operation failed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property returns true when the operation represented by this result
    ///         encountered an error, and false when it completed successfully.
    ///     </para>
    ///     <para>
    ///         It is the logical complement of <see cref="IsSuccess"/>, provided for
    ///         readability in certain contexts:
    ///         <code>
    ///         var result = await SomeOperation();
    ///         if (result.IsFailure)
    ///         {
    ///             return Result.Failure(result.Error);
    ///         }
    ///         </code>
    ///     </para>
    ///     <para>
    ///         When IsFailure is true, the <see cref="Error"/> property will contain
    ///         a non-empty error message describing the failure, and <see cref="Exception"/>
    ///         may contain an exception if the failure was caused by one.
    ///     </para>
    /// </remarks>
    bool IsFailure { get; }

    /// <summary>
    ///     Gets the error message when the operation fails.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property returns a human-readable message describing the error
    ///         that occurred when the operation failed. It is valid only when
    ///         <see cref="IsFailure"/> is true.
    ///     </para>
    ///     <para>
    ///         When <see cref="IsSuccess"/> is true, this property typically returns
    ///         an empty string.
    ///     </para>
    ///     <para>
    ///         Error messages should be descriptive enough to understand what went wrong,
    ///         but should not include sensitive information:
    ///         <code>
    ///         var result = await SomeOperation();
    ///         if (result.IsFailure)
    ///         {
    ///             Log.Error("Operation failed: {ErrorMessage}", result.Error);
    ///             return Result.Failure($"Processing error: {result.Error}");
    ///         }
    ///         </code>
    ///     </para>
    /// </remarks>
    string Error { get; }

    /// <summary>
    ///     Gets the exception that caused the failure, if any.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property returns the exception that caused the operation to fail,
    ///         if the failure was due to an exception. It is valid only when
    ///         <see cref="IsFailure"/> is true, and may be null even in failure cases
    ///         if the failure was not caused by an exception.
    ///     </para>
    ///     <para>
    ///         When <see cref="IsSuccess"/> is true, this property typically returns null.
    ///     </para>
    ///     <para>
    ///         The exception can be used for detailed logging, debugging, or specialized
    ///         error handling based on exception type:
    ///         <code>
    ///         var result = await SomeOperation();
    ///         if (result.IsFailure)
    ///         {
    ///             if (result.Exception is TimeoutException)
    ///             {
    ///                 // Handle timeout specifically
    ///             }
    ///             else
    ///             {
    ///                 // Handle other errors
    ///             }
    ///         }
    ///         </code>
    ///     </para>
    /// </remarks>
    Exception? Exception { get; }
}

/// <summary>
///     Extends the <see cref="IResult"/> interface to include a strongly-typed value
///     that is available when the operation succeeds.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation when successful</typeparam>
/// <remarks>
///     <para>
///         The <see cref="IResult{T}"/> interface represents the outcome of an operation
///         that produces a value when successful. It combines the error-handling capabilities
///         of <see cref="IResult"/> with the ability to return a strongly-typed result.
///     </para>
///     <para>
///         This pattern is particularly useful for operations that need to return data,
///         as it eliminates the need for out parameters or nullable return types to handle
///         failure cases. Instead, the result object itself contains both the success/failure
///         status and either the return value or error information.
///     </para>
///     <para>
///         The covariant type parameter (<c>out T</c>) allows for implicit conversion from
///         more specific result types to more general ones. For example, a 
///         <see cref="IResult{CustomerOrder}"/> can be assigned to a <see cref="IResult{Order}"/>
///         if CustomerOrder inherits from Order.
///     </para>
///     <para>
///         In the Chainer library, this interface is the return type of chain handler methods,
///         allowing handlers to both process the context object and signal success or failure:
///         <code>
///         public Task&lt;Result&lt;OrderContext&gt;&gt; Handle(OrderContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
///         {
///             if (!IsValid(context))
///             {
///                 return Task.FromResult(Result&lt;OrderContext&gt;.Failure("Order validation failed"));
///             }
///             
///             // Process the context
///             context.Status = OrderStatus.Validated;
///             
///             return Task.FromResult&lt;Result&lt;OrderContext&gt;&gt;(context);
///         }
///         </code>
///     </para>
/// </remarks>
public interface IResult<out T> : IResult
{
    /// <summary>
    ///     Gets the value produced by the successful operation.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property returns the result value of the operation. It is valid only
    ///         when <see cref="IResult.IsSuccess"/> is true. Accessing this property when
    ///         <see cref="IResult.IsFailure"/> is true may throw an exception, depending
    ///         on the implementation.
    ///     </para>
    ///     <para>
    ///         When handling results, always check <see cref="IResult.IsSuccess"/> before
    ///         accessing the Value property:
    ///         <code>
    ///         var result = await SomeOperation();
    ///         if (result.IsSuccess)
    ///         {
    ///             var value = result.Value;
    ///             // Use the value...
    ///         }
    ///         </code>
    ///     </para>
    ///     <para>
    ///         Implementations may provide additional safe access methods like TryGetValue
    ///         to avoid the need for explicit success checks.
    ///     </para>
    ///     <para>
    ///         In chain handlers, the Value property typically contains the processed context
    ///         object when the handler succeeds, allowing it to be passed to the next handler
    ///         in the chain.
    ///     </para>
    /// </remarks>
    T Value { get; }
}