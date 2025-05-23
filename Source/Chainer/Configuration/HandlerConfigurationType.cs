namespace Chainer.Configuration;

/// <summary>
///     Defines the supported configuration data formats for chain handlers.
/// </summary>
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