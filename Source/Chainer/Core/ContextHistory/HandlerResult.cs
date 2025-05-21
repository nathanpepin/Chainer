namespace Chainer.Core.ContextHistory;

/// <summary>
///     Captures the execution details and context state for a single handler in a chain.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="HandlerResult{TContext}"/> record represents a complete execution snapshot
///         of a single handler within a chain. It captures all relevant information about the handler's
///         execution, including:
///         <list type="bullet">
///             <item>The identity of the handler that executed</item>
///             <item>The state of the context after handler execution</item>
///             <item>The timing of the execution (start, end, and duration)</item>
///         </list>
///     </para>
///     <para>
///         This record is primarily used by <see cref="ContextHistoryResult{TContext}"/> to build
///         a comprehensive execution history for a chain. Each handler in the chain produces one
///         <see cref="HandlerResult{TContext}"/> record, which is then collected into the complete
///         history.
///     </para>
///     <para>
///         The immutable nature of this record ensures that execution history is preserved accurately,
///         making it ideal for:
///         <list type="bullet">
///             <item>Diagnostics and troubleshooting</item>
///             <item>Performance analysis and timing</item>
///             <item>Auditing and compliance tracking</item>
///             <item>Detailed execution visualization</item>
///         </list>
///     </para>
///     <para>
///         The context state captured in this record is typically a clone of the actual context
///         at the time of handler completion, ensuring that the history reflects the precise state
///         at that point in time, regardless of subsequent modifications by other handlers. This
///         cloning behavior can be controlled via the <c>doNotCloneContext</c> parameter in
///         <see cref="ChainExecutor{TContext}.ExecuteWithHistory"/> and similar methods.
///     </para>
///     <para>
///         Example usage:
///         <code>
///         // Create a handler result directly (typically done by the framework)
///         var result = new HandlerResult&lt;OrderContext&gt;(
///             "MyNamespace.ValidationHandler",
///             orderContext,
///             DateTime.UtcNow.AddMilliseconds(-50),
///             DateTime.UtcNow
///         );
///         
///         // Access timing information
///         Console.WriteLine($"Handler {result.Handler} took {result.Duration.TotalMilliseconds}ms");
///         
///         // Examine context state at completion
///         Console.WriteLine($"Order status after handler: {result.Context.Status}");
///         </code>
///     </para>
/// </remarks>
/// <typeparam name="TContext">
///     The type of context that was processed by the handler. Must be a class that
///     implements <see cref="ICloneable"/> and has a parameterless constructor.
/// </typeparam>
/// <param name="Handler">
///     The fully qualified type name of the handler that executed. This typically comes from
///     <c>handler.GetType().FullName</c> and uniquely identifies the handler class.
/// </param>
/// <param name="Context">
///     The context object after handler execution. When context cloning is enabled, this
///     is a clone of the original context, preserving the exact state at the point of
///     handler completion.
/// </param>
/// <param name="Start">
///     The timestamp when the handler began execution. This is typically captured immediately
///     before the handler's <see cref="IChainHandler{TContext}.Handle"/> method is invoked.
/// </param>
/// <param name="End">
///     The timestamp when the handler completed execution. This is typically captured immediately
///     after the handler's <see cref="IChainHandler{TContext}.Handle"/> method returns.
/// </param>
public record HandlerResult<TContext>(string Handler, TContext Context, DateTime Start, DateTime End)
    where TContext : class, ICloneable, new()
{
    /// <summary>
    ///     Gets the total time taken by the handler to execute.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This calculated property provides the exact duration of the handler's execution
    ///         by subtracting the <see cref="Start"/> time from the <see cref="End"/> time.
    ///         It offers a convenient way to access timing information for performance analysis
    ///         and monitoring.
    ///     </para>
    ///     <para>
    ///         The duration includes the complete execution time of the handler, including any
    ///         asynchronous operations that may have occurred during the <see cref="IChainHandler{TContext}.Handle"/>
    ///         method. It represents wall-clock time, not CPU time, so it may include time spent
    ///         waiting for I/O, network operations, or other asynchronous processes.
    ///     </para>
    ///     <para>
    ///         This property is particularly useful for:
    ///         <list type="bullet">
    ///             <item>Identifying performance bottlenecks in a chain</item>
    ///             <item>Setting performance baselines and monitoring trends</item>
    ///             <item>Comparing different handler implementations</item>
    ///             <item>Detecting anomalously slow executions</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Example usage:
    ///         <code>
    ///         // Find the slowest handler in a chain
    ///         var slowestHandler = historyResult.History
    ///             .OrderByDescending(h => h.Duration)
    ///             .FirstOrDefault();
    ///             
    ///         if (slowestHandler != null)
    ///         {
    ///             Console.WriteLine($"Slowest handler: {slowestHandler.Handler}");
    ///             Console.WriteLine($"Duration: {slowestHandler.Duration.TotalMilliseconds}ms");
    ///         }
    ///         </code>
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     A <see cref="TimeSpan"/> representing the duration between the Start and End times.
    /// </returns>
    public TimeSpan Duration => End - Start;
}