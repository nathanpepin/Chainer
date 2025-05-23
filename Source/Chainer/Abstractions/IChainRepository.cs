using Chainer.Configuration;
using Chainer.Execution;
using Chainer.Persistence;

namespace Chainer.Abstractions;

/// <summary>
///     Defines the contract for storing and retrieving chain definitions and execution logs,
///     providing a persistence abstraction that can be implemented for various storage technologies.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="IChainRepository" /> interface is a core component of Chainer's dynamic
///         execution system. It serves as an abstraction layer between the chain execution logic
///         and the underlying storage mechanism, enabling:
///         <list type="bullet">
///             <item>Centralized storage of chain definitions that can be modified without code changes</item>
///             <item>Persistence of execution history for auditing and analysis</item>
///             <item>Sharing of chain definitions across application instances</item>
///             <item>Runtime retrieval of chain configurations based on identifiers</item>
///         </list>
///     </para>
///     <para>
///         This interface is designed to be technology-agnostic, allowing for implementations
///         using various storage technologies:
///         <list type="bullet">
///             <item>Relational databases (SQL Server, PostgreSQL, etc.)</item>
///             <item>Document databases (MongoDB, CosmosDB, etc.)</item>
///             <item>File-based storage (JSON files, XML files)</item>
///             <item>In-memory storage for testing or simple scenarios</item>
///         </list>
///     </para>
///     <para>
///         The interface provides methods for both chain definition management (storing and
///         retrieving <see cref="ChainMessage" /> objects) and execution tracking (storing and
///         updating <see cref="ChainExecutionLog" /> records). This separation of concerns
///         allows for distinct storage strategies for definitions and execution logs if desired.
///     </para>
///     <para>
///         Chain repositories are typically registered in the dependency injection container
///         and injected into the <see cref="DynamicChainExecutor" />, which uses them to:
///         <list type="bullet">
///             <item>Load chain definitions for execution</item>
///             <item>Record the progress and results of chain execution</item>
///             <item>Track execution metrics and performance</item>
///         </list>
///     </para>
///     <para>
///         Implementations should consider performance, concurrency, and transaction handling
///         appropriate to their storage technology. For database implementations, it's recommended
///         to use appropriate indexing strategies for chain IDs and execution status to optimize
///         query performance.
///     </para>
/// </remarks>
public interface IChainRepository
{
    /// <summary>
    ///     Retrieves the messages that define the default chain.
    /// </summary>
    Task<Result<List<ChainMessage>>> GetDefaultChainMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the messages that define a chain identified by a string identifier.
    /// </summary>
    /// <param name="chainId">A string that uniquely identifies the chain</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result containing
    ///     the list of chain messages or an error
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is a convenience wrapper that converts the string identifier
    ///         to a deterministic GUID using <see cref="GuidFromString.CreateDeterministicGuid(string)" />
    ///         and then calls <see cref="GetChainMessagesAsync(Guid, CancellationToken)" />.
    ///     </para>
    ///     <para>
    ///         Using string identifiers makes chains more accessible in configuration files
    ///         and APIs, where human-readable identifiers are preferred over GUIDs.
    ///         The conversion to a deterministic GUID ensures that the same string always
    ///         maps to the same GUID, maintaining consistency.
    ///     </para>
    ///     <para>
    ///         Implementations should:
    ///         <list type="bullet">
    ///             <item>Convert the string to a deterministic GUID</item>
    ///             <item>Delegate to the GUID-based method for actual retrieval</item>
    ///             <item>Maintain consistent string-to-GUID mapping</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Examples of string identifiers might include "OrderProcessingChain",
    ///         "PaymentValidation", or "CustomerRegistrationWorkflow".
    ///     </para>
    /// </remarks>
    Task<Result<List<ChainMessage>>> GetChainMessagesAsync(string chainId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the messages that define a chain identified by a GUID.
    /// </summary>
    /// <param name="chainId">The GUID that uniquely identifies the chain</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result containing
    ///     the list of chain messages or an error
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This is the core method for retrieving chain definitions. It should return
    ///         all <see cref="ChainMessage" /> objects associated with the specified ChainId,
    ///         ordered by their ExecutionOrder property.
    ///     </para>
    ///     <para>
    ///         This method is used by the <see cref="DynamicChainExecutor" /> to retrieve
    ///         chain definitions for execution. The returned messages define the sequence
    ///         of handlers to be executed in the chain.
    ///     </para>
    ///     <para>
    ///         Implementations should:
    ///         <list type="bullet">
    ///             <item>Return an empty list if no messages exist for the specified ChainId (not an error)</item>
    ///             <item>Return a failure result if an error occurs during retrieval</item>
    ///             <item>Ensure messages are sorted by their ExecutionOrder property</item>
    ///             <item>Use appropriate caching strategies for frequently accessed chains</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         For database implementations, this typically translates to a query like:
    ///         <code>
    ///         SELECT * FROM ChainMessages WHERE ChainId = @chainId ORDER BY ExecutionOrder ASC
    ///         </code>
    ///     </para>
    /// </remarks>
    Task<Result<List<ChainMessage>>> GetChainMessagesAsync(Guid chainId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Saves a collection of messages to the default chain.
    /// </summary>
    /// <param name="messages">The chain messages to save</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is a convenience wrapper that saves messages to the default chain
    ///         identified by <see cref="InMemoryChainRepository.DefaultChainGuid" />. It modifies
    ///         the ChainId property of each message to ensure they all belong to the default chain.
    ///     </para>
    ///     <para>
    ///         This is typically used during application initialization or when programmatically
    ///         creating a default chain configuration.
    ///     </para>
    ///     <para>
    ///         Implementations should:
    ///         <list type="bullet">
    ///             <item>Set the ChainId property of each message to the default chain GUID</item>
    ///             <item>Delegate to <see cref="SaveChainMessagesAsync(Guid, IEnumerable{ChainMessage}, CancellationToken)" /> for actual storage</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    Task<Result> SaveToDefaultChainMessagesAsync(IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Saves a collection of messages to a chain identified by a string identifier.
    /// </summary>
    /// <param name="chainId">A string that uniquely identifies the chain</param>
    /// <param name="messages">The chain messages to save</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is a convenience wrapper that converts the string identifier
    ///         to a deterministic GUID using <see cref="GuidFromString.CreateDeterministicGuid(string)" />
    ///         and then calls <see cref="SaveChainMessagesAsync(Guid, IEnumerable{ChainMessage}, CancellationToken)" />.
    ///     </para>
    ///     <para>
    ///         Using string identifiers makes chains more accessible in configuration files
    ///         and APIs, where human-readable identifiers are preferred over GUIDs.
    ///         The conversion to a deterministic GUID ensures that the same string always
    ///         maps to the same GUID, maintaining consistency.
    ///     </para>
    ///     <para>
    ///         Implementations should:
    ///         <list type="bullet">
    ///             <item>Convert the string to a deterministic GUID</item>
    ///             <item>Ensure all messages have their ChainId property set to this GUID</item>
    ///             <item>Delegate to the GUID-based method for actual storage</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    Task<Result> SaveChainMessagesAsync(string chainId, IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Saves a collection of messages to a chain identified by a GUID.
    /// </summary>
    /// <param name="chainId">The GUID that uniquely identifies the chain</param>
    /// <param name="messages">The chain messages to save</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This is the core method for storing chain definitions. It should save
    ///         all provided <see cref="ChainMessage" /> objects, ensuring they are associated
    ///         with the specified chainId.
    ///     </para>
    ///     <para>
    ///         This method is used for:
    ///         <list type="bullet">
    ///             <item>Creating new chain definitions</item>
    ///             <item>Updating existing chain definitions</item>
    ///             <item>Adding handlers to existing chains</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Implementations should:
    ///         <list type="bullet">
    ///             <item>Set the ChainId property of each message to the specified chainId</item>
    ///             <item>Save each message to the underlying storage</item>
    ///             <item>Handle duplicate messages appropriately (update or reject)</item>
    ///             <item>Apply appropriate transactional guarantees</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Storage behavior for existing messages can vary by implementation:
    ///         <list type="bullet">
    ///             <item>Some implementations might append new messages to existing ones</item>
    ///             <item>Others might replace existing messages with matching Ids</item>
    ///             <item>Some might reject duplicate Ids with an error</item>
    ///         </list>
    ///         The implementation should document its behavior for clarity.
    ///     </para>
    ///     <para>
    ///         For database implementations, this typically involves inserting or updating
    ///         records in a ChainMessages table, with appropriate handling of transactions
    ///         and concurrency.
    ///     </para>
    /// </remarks>
    Task<Result> SaveChainMessagesAsync(Guid chainId, IEnumerable<ChainMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Saves a collection of chain execution logs to the repository.
    /// </summary>
    /// <param name="chainExecutionLogs">The execution logs to save</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is used to record the initial state of execution logs at the
    ///         beginning of a chain execution. It's typically called by the <see cref="DynamicChainExecutor" />
    ///         before any handlers are executed.
    ///     </para>
    ///     <para>
    ///         The logs provided to this method usually have their status set to
    ///         <see cref="ChainMessageStatus.Pending" /> and contain basic information about
    ///         the handlers to be executed. As execution progresses, these logs are updated
    ///         using the <see cref="UpdateChainExecutionLog(ChainExecutionLog, ChainMessageStatus, CancellationToken)" /> methods.
    ///     </para>
    ///     <para>
    ///         Implementations should:
    ///         <list type="bullet">
    ///             <item>Save each log to the underlying storage</item>
    ///             <item>Preserve the Id property of each log for later updates</item>
    ///             <item>Handle duplicate logs appropriately (update or reject)</item>
    ///             <item>Apply appropriate transactional guarantees</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         For database implementations, this typically involves inserting records
    ///         into a ChainExecutionLogs table, with appropriate handling of transactions
    ///         and concurrency.
    ///     </para>
    /// </remarks>
    Task<Result> SaveChainExecutionLogs(IEnumerable<ChainExecutionLog> chainExecutionLogs, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates the status of a chain execution log.
    /// </summary>
    /// <param name="chainExecutionLog">The log to update</param>
    /// <param name="status">The new status to set</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is used to update the status of an execution log as the handler
    ///         progresses through its lifecycle. It's typically called by the <see cref="DynamicChainExecutor" />
    ///         at key points during execution:
    ///         <list type="bullet">
    ///             <item>Before execution begins (Pending → Executing)</item>
    ///             <item>After successful completion (Executing → Completed)</item>
    ///             <item>After failure (Executing → Failed or Error)</item>
    ///             <item>When skipping due to previous failure (Pending → Skipped)</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Implementations should:
    ///         <list type="bullet">
    ///             <item>Update the Status property of the log to the specified value</item>
    ///             <item>Preserve all other properties of the log</item>
    ///             <item>Save the updated log to the underlying storage</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This method provides a simplified update path for cases where only the status
    ///         needs to be changed. For more complex updates involving context data, use the
    ///         overload that accepts before/after JSON parameters.
    ///     </para>
    /// </remarks>
    Task<Result> UpdateChainExecutionLog(
        ChainExecutionLog chainExecutionLog,
        ChainMessageStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates a chain execution log with a new status and context data.
    /// </summary>
    /// <param name="chainExecutionLog">The log to update</param>
    /// <param name="status">The new status to set</param>
    /// <param name="beforeExecution">The JSON-serialized context state before execution</param>
    /// <param name="afterExecution">The JSON-serialized context state after execution</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is an extension of <see cref="UpdateChainExecutionLog(ChainExecutionLog, ChainMessageStatus, CancellationToken)" />
    ///         that also updates the before and after context state. It's typically used when
    ///         a handler implements <see cref="IPostExecutionPersistence" /> and/or <see cref="IPreExecutionPersistence" />.
    ///     </para>
    ///     <para>
    ///         The context state is captured as JSON strings to maintain database compatibility
    ///         and preserve the complete state for analysis. This enables:
    ///         <list type="bullet">
    ///             <item>Tracking changes made by each handler</item>
    ///             <item>Debugging issues by examining the context state</item>
    ///             <item>Auditing the data processed by the chain</item>
    ///             <item>Recreating the execution environment for testing</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Implementations should:
    ///         <list type="bullet">
    ///             <item>Update the Status property of the log to the specified value</item>
    ///             <item>Update the BeforeJson property if beforeExecution is not null</item>
    ///             <item>Update the AfterJson property to the specified value</item>
    ///             <item>Save the updated log to the underlying storage</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         For database implementations, this typically involves updating a record
    ///         in the ChainExecutionLogs table, with appropriate handling of large JSON
    ///         strings in the database (e.g., using appropriate column types).
    ///     </para>
    /// </remarks>
    Task<Result> UpdateChainExecutionLog(
        ChainExecutionLog chainExecutionLog,
        ChainMessageStatus status,
        string? beforeExecution,
        string afterExecution,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates a collection of chain execution logs.
    /// </summary>
    /// <param name="chainExecutionLogs">The logs to update</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a result indicating
    ///     success or failure
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method provides a batch update mechanism for execution logs. It's useful
    ///         for scenarios where multiple logs need to be updated in a single operation,
    ///         such as marking all remaining handlers as skipped after a failure.
    ///     </para>
    ///     <para>
    ///         Implementations should:
    ///         <list type="bullet">
    ///             <item>Update each log in the collection according to its current state</item>
    ///             <item>Apply the updates as efficiently as possible (e.g., bulk update)</item>
    ///             <item>Apply appropriate transactional guarantees</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         For database implementations, this might involve a bulk update operation
    ///         or a transaction containing multiple individual updates, depending on the
    ///         capabilities of the database system.
    ///     </para>
    /// </remarks>
    Task<Result> UpdateChainExecutionLog(IEnumerable<ChainExecutionLog> chainExecutionLogs, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the execution logs for a chain identified by a GUID.
    /// </summary>
    /// <param name="chainId">The GUID that uniquely identifies the chain</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a list of execution logs
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method retrieves all execution logs associated with a specific chain execution.
    ///         It's typically used for:
    ///         <list type="bullet">
    ///             <item>Analyzing the results of a chain execution</item>
    ///             <item>Debugging issues in chain execution</item>
    ///             <item>Auditing the processing of a specific item</item>
    ///             <item>Generating execution reports</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Implementations should:
    ///         <list type="bullet">
    ///             <item>Return all logs associated with the specified chainId</item>
    ///             <item>Order the logs by their ExecutionOrder property</item>
    ///             <item>Return an empty list if no logs exist for the specified chainId</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Note that unlike the Get methods for chain messages, this method returns a direct
    ///         List rather than a Result. This reflects its usage pattern, where the absence of
    ///         logs is not considered an error condition.
    ///     </para>
    ///     <para>
    ///         For database implementations, this typically translates to a query like:
    ///         <code>
    ///         SELECT * FROM ChainExecutionLogs WHERE ChainId = @chainId ORDER BY ExecutionOrder ASC
    ///         </code>
    ///     </para>
    /// </remarks>
    Task<List<ChainExecutionLog>> GetChainExecutionLogs(Guid chainId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the execution logs for the default chain.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>
    ///     A task representing the asynchronous operation, with a list of execution logs
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is a convenience wrapper that retrieves execution logs for the
    ///         default chain identified by <see cref="InMemoryChainRepository.DefaultChainGuid" />.
    ///     </para>
    ///     <para>
    ///         It delegates to <see cref="GetChainExecutionLogs(Guid, CancellationToken)" />
    ///         with the default chain GUID, providing a simpler interface for accessing
    ///         the execution history of the default chain.
    ///     </para>
    ///     <para>
    ///         This is particularly useful in applications that primarily use the default
    ///         chain for processing, allowing for simpler code when accessing execution history.
    ///     </para>
    ///     <para>
    ///         Implementations should simply delegate to the GUID-based method with the
    ///         default chain GUID.
    ///     </para>
    /// </remarks>
    Task<List<ChainExecutionLog>> GetDefaultChainExecutionLogs(CancellationToken cancellationToken = default);
}