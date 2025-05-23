namespace Chainer.Configuration;

/// <summary>
///     Defines the supported configuration data formats for chain handlers.
/// </summary>
/// <remarks>
///     This enum is used throughout the chain handling system to indicate how configuration data
///     should be interpreted and processed. It determines which parser will be used when binding
///     configuration data to handler configuration objects.
///     The ChainHandlerConfiguration class uses this enum to track which internal storage field
///     contains the active configuration data and to select the appropriate parsing method
///     during binding operations.
/// </remarks>
public enum HandlerConfigurationType
{
    /// <summary>
    ///     Indicates that no configuration format has been set or that configuration data is not present.
    /// </summary>
    NotSet = 0,

    /// <summary>
    ///     Indicates that the configuration data is a direct object reference.
    /// </summary>
    Object,

    /// <summary>
    ///     Indicates that the configuration data is formatted as a JSON string.
    /// </summary>
    Json,

    /// <summary>
    ///     Indicates that the configuration data is stored as a dictionary of key-value pairs.
    /// </summary>
    Dictionary,

    /// <summary>
    ///     Indicates that the configuration data is formatted as an XML string.
    /// </summary>
    Xml
}