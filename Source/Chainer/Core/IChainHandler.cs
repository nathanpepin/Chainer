using Microsoft.Extensions.Logging;

namespace Chainer.Core;

/// <summary>
///     Defines a single processing step in a chain of responsibility pattern.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="IChainHandler{TContext}"/> interface represents a fundamental building block
///         in the Chainer library's implementation of the Chain of Responsibility pattern. Each handler
///         represents a discrete processing step that can examine, modify, or validate a context object
///         as it flows through a processing chain.
///     </para>
///     <para>
///         Handlers form the individual links in a processing chain. When combined in sequence, they
///         create a pipeline that progressively transforms or processes a shared context object. Each
///         handler's responsibility should be focused on a single concern, following the Single
///         Responsibility Principle.
///     </para>
///     <para>
///         Common use cases for handlers include:
///         <list type="bullet">
///             <item>Validating input or state (e.g., checking that required fields are present)</item>
///             <item>Transforming data (e.g., converting between formats or enriching with additional information)</item>
///             <item>Performing business operations (e.g., calculating prices or processing payments)</item>
///             <item>Logging or auditing (e.g., recording that certain operations occurred)</item>
///             <item>Integration with external systems (e.g., making API calls or database operations)</item>
///         </list>
///     </para>
///     <para>
///         Handlers communicate success or failure through the <see cref="Result{TContext}"/> return type,
///         which encapsulates either a successful result with the modified context or a failure with
///         an error message. This approach eliminates the need for exception handling while providing
///         a clear path for error propagation.
///     </para>
///     <para>
///         Implementations should:
///         <list type="bullet">
///             <item>Focus on a single responsibility</item>
///             <item>Be stateless whenever possible to ensure thread safety</item>
///             <item>Properly handle the cancellation token for cooperative cancellation</item>
///             <item>Use the logger for diagnostic information if provided</item>
///             <item>Return meaningful error messages when failures occur</item>
///         </list>
///     </para>
///     <para>
///         Example implementation:
///         <code>
///         public class ValidationHandler : IChainHandler&lt;OrderContext&gt;
///         {
///             public Task&lt;Result&lt;OrderContext&gt;&gt; Handle(
///                 OrderContext context,
///                 ILogger? logger = null,
///                 CancellationToken cancellationToken = default)
///             {
///                 logger?.LogInformation("Validating order {OrderId}", context.OrderId);
///                 
///                 if (string.IsNullOrEmpty(context.CustomerName))
///                 {
///                     return Task.FromResult(Result&lt;OrderContext&gt;.Failure("Customer name is required"));
///                 }
///                 
///                 if (context.Items.Count == 0)
///                 {
///                     return Task.FromResult(Result&lt;OrderContext&gt;.Failure("Order must contain at least one item"));
///                 }
///                 
///                 return Task.FromResult(Result&lt;OrderContext&gt;.Success(context));
///             }
///         }
///         </code>
///     </para>
///     <para>
///         Handlers can be composed into chains using <see cref="ChainExecutor{TContext}"/> or
///         <see cref="ChainService{TContext}"/>, or dynamically configured using
///         <see cref="DynamicChainExecutor"/>. For handlers that require configuration, consider
///         implementing <see cref="IConfigurableChainHandler{TContext}"/> instead.
///     </para>
/// </remarks>
/// <typeparam name="TContext">
///     The type of context object that this handler can process. Must be a class that
///     implements <see cref="ICloneable"/> and has a parameterless constructor.
/// </typeparam>
public interface IChainHandler<TContext> where TContext : class, ICloneable, new()
{
    /// <summary>
    ///     Processes the provided context, applying the handler's specific logic.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method implements the core processing logic for the handler. It receives
    ///         a context object, applies some transformation or validation to it, and returns
    ///         either a success result with the modified context or a failure result with an
    ///         error message.
    ///     </para>
    ///     <para>
    ///         The method should be designed to work within an asynchronous context, even if the
    ///         actual implementation is synchronous. For synchronous operations, return the result
    ///         wrapped in a completed task:
    ///         <code>
    ///         return Task.FromResult(Result&lt;TContext&gt;.Success(context));
    ///         </code>
    ///     </para>
    ///     <para>
    ///         For failure cases, the method should construct an appropriate error message and
    ///         return it as a failure result:
    ///         <code>
    ///         return Task.FromResult(Result&lt;TContext&gt;.Failure("Validation failed: Invalid state"));
    ///         </code>
    ///     </para>
    ///     <para>
    ///         If the operation might take a significant amount of time or involves I/O operations
    ///         (network requests, database queries, etc.), implement it as a true asynchronous
    ///         method using async/await:
    ///         <code>
    ///         // Asynchronous example
    ///         public async Task&lt;Result&lt;TContext&gt;&gt; Handle(
    ///             TContext context,
    ///             ILogger? logger = null,
    ///             CancellationToken cancellationToken = default)
    ///         {
    ///             try
    ///             {
    ///                 // Check for cancellation before expensive operations
    ///                 cancellationToken.ThrowIfCancellationRequested();
    ///                 
    ///                 var data = await _dataService.FetchDataAsync(context.Id, cancellationToken);
    ///                 context.EnrichmentData = data;
    ///                 
    ///                 logger?.LogInformation("Successfully enriched context {ContextId}", context.Id);
    ///                 return Result&lt;TContext&gt;.Success(context);
    ///             }
    ///             catch (OperationCanceledException)
    ///             {
    ///                 // Propagate cancellation
    ///                 throw;
    ///             }
    ///             catch (Exception ex)
    ///             {
    ///                 logger?.LogError(ex, "Failed to enrich context {ContextId}", context.Id);
    ///                 return Result&lt;TContext&gt;.Failure($"Enrichment failed: {ex.Message}");
    ///             }
    ///         }
    ///         </code>
    ///     </para>
    ///     <para>
    ///         The method should respect the cancellation token and implement cooperative
    ///         cancellation, especially for long-running operations. When a cancellation is
    ///         requested, the operation should be terminated as soon as possible, either by:
    ///         <list type="bullet">
    ///             <item>Throwing an OperationCanceledException</item>
    ///             <item>Passing the token to downstream asynchronous operations</item>
    ///             <item>Checking the token periodically during long-running operations</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         When the logger is provided, it should be used to log relevant information about
    ///         the handler's execution, such as:
    ///         <list type="bullet">
    ///             <item>Start and completion of significant processing steps</item>
    ///             <item>Important decisions or state changes within the handler</item>
    ///             <item>Warnings about potential issues or edge cases</item>
    ///             <item>Detailed information about failures when they occur</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <param name="context">
    ///     The context object to be processed by this handler. This contains the data that
    ///     will be examined, validated, or modified during processing.
    /// </param>
    /// <param name="logger">
    ///     An optional logger for recording diagnostic information. If provided, the handler
    ///     should log appropriate information about its execution. If null, logging should
    ///     be skipped without affecting the core functionality.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token for monitoring cancellation requests. The handler should check this token
    ///     and abort processing if cancellation is requested, implementing cooperative
    ///     cancellation patterns.
    /// </param>
    /// <returns>
    ///     A task that resolves to a <see cref="Result{TContext}"/> containing either:
    ///     <list type="bullet">
    ///         <item>A success result with the processed context (possibly modified), or</item>
    ///         <item>A failure result with an error message explaining why processing failed</item>
    ///     </list>
    /// </returns>
    Task<Result<TContext>> Handle(TContext context, ILogger? logger = null, CancellationToken cancellationToken = default);
}