namespace Chainer.ChainServices.ChainBuilder.Messages;

public sealed class ChainMessageRecord
{
    public Guid Id { get; set; }

    // Chain identification
    public Guid ChainId { get; set; }
    public int ExecutionOrder { get; set; }

    // Handler information
    public string HandlerTypeName { get; set; } = string.Empty;

    // Configuration - stored as JSON in the database
    public string? ConfigurationJson { get; set; }

    // Type information
    public string ContextTypeName { get; set; } = string.Empty;
}