namespace Chainer.ChainServices.ChainBuilder.Messages;

public sealed class ChainExecutionLog
{
    public Guid Id { get; set; }

    // Chain identification
    public Guid ChainId { get; set; }
    public int ExecutionOrder { get; set; }

    // Handler information
    public string HandlerTypeName { get; set; } = string.Empty;

    // Configuration - stored as JSON in the database
    public string? ConfigurationJson { get; set; }

    // Status tracking
    public ChainMessageStatus Status { get; set; } = ChainMessageStatus.Pending;
    public DateTime? ExecutedAt { get; set; }
    public string? ErrorMessage { get; set; }

    // Type information
    public string ContextTypeName { get; set; } = string.Empty;

    public string ContextJson { get; set; } = string.Empty;
}