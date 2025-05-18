namespace Chainer.ChainServices.ChainBuilder.Messages;

/// <summary>
/// Symbolizes a log of a chain's execution
/// </summary>
public sealed class ChainExecutionLog
{
    public ChainExecutionLog()
    {
    }

    public ChainExecutionLog(ChainMessage message)
    {
        Id = message.Id;
        ChainId = message.ChainId;
        ExecutionOrder = message.ExecutionOrder;
        HandlerTypeName = message.HandlerTypeName;
        ConfigurationJson = message.ConfigurationJson;
        ContextTypeName = message.ContextTypeName;
        BeforeJson = message.ConfigurationJson;
        Status = ChainMessageStatus.Pending;
    }


    public Guid Id { get; set; }

    // Chain identification
    public Guid ChainId { get; set; }
    public int ExecutionOrder { get; set; }

    // Handler information
    public string HandlerTypeName { get; set; } = string.Empty;

    // Configuration - stored as JSON in the database
    public string? ConfigurationJson { get; set; }

    // Status tracking
    public ChainMessageStatus Status { get; set; } = ChainMessageStatus.NotStarted;
    public DateTime? ExecutedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? ErrorMessage { get; set; }

    // Type information
    public string ContextTypeName { get; set; } = string.Empty;

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }
}