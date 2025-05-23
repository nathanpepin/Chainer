using Chainer.Execution;
using Microsoft.Extensions.Logging;

namespace Chainer.Abstractions;

/// <summary>
///     Defines a single processing step in a chain of responsibility pattern.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="IChainHandler{TContext}" /> interface represents a fundamental building block
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
///         Handlers communicate success or failure through the <see cref="Result{TContext}" /> return type,
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
///         Handlers can be composed into chains using <see cref="ChainExecutor{TContext}" /> or
///         <see cref="DependencyInjectedChainExecutor{TContext}" />, or dynamically configured using
///         <see cref="DynamicChainExecutor" />. For handlers that require configuration, consider
///         implementing <see cref="IConfigurableChainHandler{TContext}" /> instead.
///     </para>
/// </remarks>
/// <typeparam name="TContext">
///     The type of context object that this handler can process. Must be a class that
///     implements <see cref="ICloneable" /> and has a parameterless constructor.
/// </typeparam>
public interface IChainHandler<TContext> where TContext : class, ICloneable, new()
{
    /// <summary>
    ///     Processes the provided context, applying the handler's specific logic.
    /// </summary>
    Task<Result<TContext>> Handle(TContext context, ILogger? logger = null, CancellationToken cancellationToken = default);
}