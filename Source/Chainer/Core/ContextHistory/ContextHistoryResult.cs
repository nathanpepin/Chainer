using System.Text;

namespace Chainer.Core.ContextHistory;

/// <summary>
///     Provides detailed execution history and metadata for a complete chain execution.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="ContextHistoryResult{TContext}"/> class serves as a comprehensive record
///         of a chain's execution, capturing both the final result and detailed metadata about the
///         execution process. It includes timing information, the sequence of handlers, their
///         execution status, and the evolution of the context throughout the chain.
///     </para>
///     <para>
///         This class provides valuable insights for:
///         <list type="bullet">
///             <item>Debugging and troubleshooting chain execution issues</item>
///             <item>Performance analysis and optimization</item>
///             <item>Auditing and compliance tracking</item>
///             <item>Visualization of the execution flow</item>
///             <item>Understanding how context state evolves through the chain</item>
///         </list>
///     </para>
///     <para>
///         The history tracks both successful and failed executions, providing a complete picture
///         of what happened during chain processing. In the case of failures, it captures which
///         handler failed and why, as well as which handlers were skipped as a result.
///     </para>
///     <para>
///         This class is typically returned by the <see cref="ChainExecutor{TContext}.ExecuteWithHistory"/>
///         and <see cref="ChainService{TContext}.ExecuteWithHistory"/> methods, providing a richer
///         alternative to the basic <see cref="Result{TContext}"/> when detailed execution tracking
///         is required.
///     </para>
///     <para>
///         Example usage:
///         <code>
///         // Execute a chain with history tracking
///         var result = await chainExecutor.ExecuteWithHistory(orderContext);
///         
///         // Display the formatted execution summary
///         Console.WriteLine(result.PrintOutput());
///         
///         // Check for overall success
///         if (result.Result.IsSuccess)
///         {
///             Console.WriteLine("Chain executed successfully");
///             var processedContext = result.Result.Value;
///             // Use the processed context...
///         }
///         else
///         {
///             Console.WriteLine($"Chain failed: {result.Result.Error}");
///             
///             // Find which handler failed
///             var lastExecutedHandler = result.History.LastOrDefault()?.Handler;
///             Console.WriteLine($"Failed at handler: {lastExecutedHandler}");
///             
///             // See which handlers were skipped
///             Console.WriteLine("Skipped handlers:");
///             foreach (var handler in result.UnappliedHandlers)
///             {
///                 Console.WriteLine($"  - {handler}");
///             }
///         }
///         
///         // Analyze performance
///         Console.WriteLine($"Total execution time: {result.ExecutionTime.TotalMilliseconds}ms");
///         
///         var slowestHandler = result.History
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
/// <typeparam name="TContext">
///     The type of context that flows through the chain. Must be a class that
///     implements <see cref="ICloneable"/> and has a parameterless constructor.
/// </typeparam>
public sealed class ContextHistoryResult<TContext> where TContext : class, ICloneable, new()
{
    /// <summary>
    ///     Gets or sets the final outcome of the chain execution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property contains the ultimate result of the chain execution, including:
    ///         <list type="bullet">
    ///             <item>Success or failure status</item>
    ///             <item>The final processed context (if successful)</item>
    ///             <item>An error message (if failed)</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         For successful executions, the <see cref="Result{TContext}.Value"/> property
    ///         contains the fully processed context after it has passed through all handlers
    ///         in the chain. This represents the final state after all transformations and
    ///         processing have been applied.
    ///     </para>
    ///     <para>
    ///         For failed executions, the <see cref="Result{TContext}.Error"/> property
    ///         contains an error message describing why the chain execution failed. This
    ///         typically comes from the handler that encountered the failure condition.
    ///     </para>
    ///     <para>
    ///         This property provides a convenient way to assess the overall outcome of
    ///         the chain execution without needing to examine the detailed history.
    ///     </para>
    /// </remarks>
    public Result<TContext> Result { get; set; }

    /// <summary>
    ///     Gets the chronological history of handler executions with their timing and context states.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This collection contains a <see cref="HandlerResult{TContext}"/> record for each
    ///         handler that was successfully executed during the chain processing. Each record
    ///         captures:
    ///         <list type="bullet">
    ///             <item>The identity of the handler</item>
    ///             <item>The state of the context after the handler completed</item>
    ///             <item>The start and end times of the handler's execution</item>
    ///             <item>The calculated duration of the handler's execution</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The records appear in the order of execution, creating a chronological history
    ///         of the chain's processing. This allows for detailed analysis of how the context
    ///         evolved through the chain and how long each processing step took.
    ///     </para>
    ///     <para>
    ///         The context state captured in each record depends on the <c>doNotCloneContext</c>
    ///         parameter used during execution:
    ///         <list type="bullet">
    ///             <item>
    ///                 When context cloning is enabled (default), each record contains a snapshot
    ///                 of the context state immediately after the handler completed, providing a
    ///                 precise view of how the context evolved.
    ///             </item>
    ///             <item>
    ///                 When context cloning is disabled, each record contains a reference to the
    ///                 final context state, which may reflect changes made by subsequent handlers.
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         If a handler fails, the history only includes records up to the point of failure.
    ///         Handlers that were skipped due to earlier failures are not represented in the history.
    ///     </para>
    /// </remarks>
    public List<HandlerResult<TContext>> History { get; } = [];

    /// <summary>
    ///     Gets the collection of all handler type names that were registered for the chain execution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This collection contains the fully qualified type names of all handlers that were
    ///         registered to be part of the chain, regardless of whether they were actually executed.
    ///         It represents the complete intended execution path of the chain.
    ///     </para>
    ///     <para>
    ///         The handlers are listed in their registration order, which typically corresponds to
    ///         their intended execution order. This collection is useful for:
    ///         <list type="bullet">
    ///             <item>Understanding the complete chain composition</item>
    ///             <item>Comparing intended vs. actual execution paths</item>
    ///             <item>Identifying which handlers were registered but not executed</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         To determine which handlers were actually executed, refer to the <see cref="History"/>
    ///         property. To identify which handlers were not executed due to failures, see the
    ///         <see cref="UnappliedHandlers"/> property.
    ///     </para>
    /// </remarks>
    public List<string> Handlers { get; } = [];

    /// <summary>
    ///     Gets the collection of handler type names that were not executed due to chain failure.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This collection contains the fully qualified type names of handlers that were
    ///         registered for execution but were not executed because a previous handler in
    ///         the chain failed. It represents the remainder of the intended execution path
    ///         that was skipped.
    ///     </para>
    ///     <para>
    ///         When a chain executes successfully, this collection remains empty, indicating
    ///         that all registered handlers were applied. When a chain fails, this collection
    ///         contains all handlers that would have executed if the failure had not occurred.
    ///     </para>
    ///     <para>
    ///         This property is particularly useful for:
    ///         <list type="bullet">
    ///             <item>Diagnosing the impact of a failure (which steps were skipped)</item>
    ///             <item>Understanding the remaining work that was not completed</item>
    ///             <item>Identifying dependencies that might be affected by the failure</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The handlers are listed in their intended execution order, starting from the
    ///         handler that would have executed immediately after the failing handler.
    ///     </para>
    /// </remarks>
    public List<string> UnappliedHandlers { get; } = [];

    /// <summary>
    ///     Gets the timestamp when the chain execution began.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property captures the precise moment when the chain execution was initiated.
    ///         It is typically set to <see cref="DateTime.UtcNow"/> at the beginning of the
    ///         <see cref="ChainExecutor{TContext}.ExecuteWithHistory"/> or similar method call.
    ///     </para>
    ///     <para>
    ///         The timestamp uses UTC time to ensure consistency across different time zones
    ///         and to facilitate accurate timing calculations, especially in distributed systems.
    ///     </para>
    ///     <para>
    ///         This property, along with <see cref="End"/>, is used to calculate the
    ///         <see cref="ExecutionTime"/> for the entire chain execution.
    ///     </para>
    /// </remarks>
    public DateTime Start { get; init; }

    /// <summary>
    ///     Gets or sets the timestamp when the chain execution completed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property captures the precise moment when the chain execution finished,
    ///         whether successfully or due to a failure. It is typically set to
    ///         <see cref="DateTime.UtcNow"/> at the end of the
    ///         <see cref="ChainExecutor{TContext}.ExecuteWithHistory"/> or similar method call.
    ///     </para>
    ///     <para>
    ///         The timestamp uses UTC time to ensure consistency across different time zones
    ///         and to facilitate accurate timing calculations, especially in distributed systems.
    ///     </para>
    ///     <para>
    ///         This property, along with <see cref="Start"/>, is used to calculate the
    ///         <see cref="ExecutionTime"/> for the entire chain execution.
    ///     </para>
    /// </remarks>
    public DateTime End { get; set; }

    /// <summary>
    ///     Gets the total duration of the chain execution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This calculated property provides the exact duration of the entire chain execution
    ///         by subtracting the <see cref="Start"/> time from the <see cref="End"/> time.
    ///         It represents the wall-clock time taken to process the complete chain, from
    ///         initialization to completion or failure.
    ///     </para>
    ///     <para>
    ///         The execution time includes:
    ///         <list type="bullet">
    ///             <item>The cumulative processing time of all executed handlers</item>
    ///             <item>Any overhead between handler executions</item>
    ///             <item>Initial setup and final cleanup operations</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This property is particularly useful for:
    ///         <list type="bullet">
    ///             <item>Performance monitoring and optimization</item>
    ///             <item>Setting baseline expectations for chain execution times</item>
    ///             <item>Detecting anomalous execution patterns</item>
    ///             <item>Calculating throughput metrics</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Note that the execution time represents wall-clock time, not CPU time, so it may
    ///         include time spent waiting for I/O, network operations, or other asynchronous
    ///         processes within handlers.
    ///     </para>
    /// </remarks>
    public TimeSpan ExecutionTime => End - Start;

    /// <summary>
    ///     Generates a formatted text representation of the execution history and results.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method creates a human-readable summary of the chain execution, including:
    ///         <list type="bullet">
    ///             <item>The context type that was processed</item>
    ///             <item>The success or failure status</item>
    ///             <item>Error details (if applicable)</item>
    ///             <item>Start and end times</item>
    ///             <item>Total execution duration</item>
    ///             <item>List of applied handlers with their individual durations</item>
    ///             <item>List of unapplied handlers (if any)</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The output is formatted as a multi-line string with clear section headers and
    ///         indentation for readability. This makes it suitable for:
    ///         <list type="bullet">
    ///             <item>Logging to console or files</item>
    ///             <item>Including in diagnostic reports</item>
    ///             <item>Displaying in debugging tools or interfaces</item>
    ///             <item>Email notifications or alerts</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The optional <paramref name="includeLineBreaks"/> parameter controls whether
    ///         decorative separator lines are included at the beginning and end of the output.
    ///         This can be useful when embedding the output in other text or when appending
    ///         multiple outputs together.
    ///     </para>
    ///     <para>
    ///         Example output:
    ///         <code>
    ///         ----------------------------------------
    ///         Context: MyNamespace.OrderContext
    ///         Success: True
    ///         Error: None
    ///         Start: 2023-06-15T10:15:30
    ///         End: 2023-06-15T10:15:31
    ///         Execution Time: 0:00:01.0234567
    ///         Applied Handlers
    ///             -MyNamespace.ValidationHandler; Duration: 0:00:00.0123456
    ///             -MyNamespace.PricingHandler; Duration: 0:00:00.0789012
    ///             -MyNamespace.NotificationHandler; Duration: 0:00:00.0321098
    ///         ----------------------------------------
    ///         </code>
    ///     </para>
    /// </remarks>
    /// <param name="includeLineBreaks">
    ///     When true (default), adds decorative separator lines at the beginning and end
    ///     of the output. When false, omits these separators for cleaner embedding in
    ///     other text.
    /// </param>
    /// <returns>
    ///     A formatted string containing the complete execution summary.
    /// </returns>
    public string PrintOutput(bool includeLineBreaks = true)
    {
        StringBuilder stringBuilder = new();

        if (includeLineBreaks)
            stringBuilder.AppendLine("----------------------------------------");

        stringBuilder.AppendLine($"Context: {typeof(TContext).FullName}");
        stringBuilder.AppendLine($"Success: {Result.IsSuccess}");

        if (Result.IsFailure)
            stringBuilder.AppendLine($"Error: {Result.Error}");
        else
            stringBuilder.AppendLine("Error: None");

        stringBuilder.AppendLine($"Start: {Start:s}");
        stringBuilder.AppendLine($"End: {End:s}");
        stringBuilder.AppendLine($"Execution Time: {ExecutionTime:g}");

        stringBuilder.AppendLine("Applied Handlers");
        foreach (var history in History) stringBuilder.AppendLine($"\t-{history.Handler}; Duration: {history.Duration:g}");

        if (UnappliedHandlers.Count != 0)
        {
            stringBuilder.AppendLine("Not Applied Handlers");
            foreach (var handler in UnappliedHandlers) stringBuilder.AppendLine($"\t-{handler}");
        }

        if (includeLineBreaks)
            stringBuilder.AppendLine("----------------------------------------");

        return stringBuilder.ToString();
    }

    /// <summary>
    ///     Returns a formatted string representation of the execution history and results.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This override of the standard <see cref="object.ToString"/> method provides a
    ///         convenient way to access the formatted execution summary. It delegates to the
    ///         <see cref="PrintOutput"/> method with default parameters to generate a complete
    ///         formatted summary.
    ///     </para>
    ///     <para>
    ///         This makes it easy to include the execution history in logs, debug output, or
    ///         other string contexts without explicitly calling <see cref="PrintOutput"/>.
    ///     </para>
    ///     <para>
    ///         Example usage:
    ///         <code>
    ///         var result = await chainExecutor.ExecuteWithHistory(context);
    ///         Console.WriteLine(result); // Automatically calls ToString()
    ///         logger.LogInformation("Chain execution completed: {Result}", result);
    ///         </code>
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     A formatted string containing the complete execution summary with line breaks.
    /// </returns>
    public override string ToString()
    {
        return PrintOutput();
    }
}