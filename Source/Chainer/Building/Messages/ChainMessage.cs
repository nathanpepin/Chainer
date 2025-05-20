using Chainer.Building.Configuration;

namespace Chainer.Building.Messages;

/// <summary>
///     A storable chain link
/// </summary>
public sealed class ChainMessage
{
    public Guid Id { get; set; }

    // Chain identification
    public Guid ChainId { get; set; }
    public string FriendlyName { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; }

    // Handler information
    public string HandlerTypeName { get; set; } = string.Empty;

    public HandlerConfigurationType ConfigurationType { get; set; } = HandlerConfigurationType.Json;

    // Configuration - stored as in the database
    public string? Configuration { get; set; }

    // Type information
    public string ContextTypeName { get; set; } = string.Empty;
}