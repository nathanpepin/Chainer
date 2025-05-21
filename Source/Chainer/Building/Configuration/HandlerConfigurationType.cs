namespace Chainer.Building.Configuration;

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
    /// <remarks>
    ///     This is the default value. A configuration with this type cannot be successfully bound.
    ///     It's primarily used as an initial state or to indicate that configuration has been reset.
    /// </remarks>
    NotSet = 0,

    /// <summary>
    ///     Indicates that the configuration data is a direct object reference.
    /// </summary>
    /// <remarks>
    ///     When this type is used, the configuration data is expected to already be an instance of
    ///     the target configuration class or a compatible object that can be cast to it.
    ///     This is the most efficient format as it requires no parsing, but it's typically only
    ///     available when configurations are created programmatically rather than loaded from external sources.
    /// </remarks>
    Object,

    /// <summary>
    ///     Indicates that the configuration data is formatted as a JSON string.
    /// </summary>
    /// <remarks>
    ///     When this type is used, the configuration data is expected to be a valid JSON string
    ///     that can be deserialized into the target configuration class.
    ///     This is the most common format for configuration data sourced from appsettings.json files
    ///     or from web APIs. It's also the default format used in ChainConfigurationGroup.ToChainMessages.
    /// </remarks>
    Json,

    /// <summary>
    ///     Indicates that the configuration data is stored as a dictionary of key-value pairs.
    /// </summary>
    /// <remarks>
    ///     When this type is used, the configuration data is expected to be an IDictionary&lt;string, string?&gt;
    ///     where keys map to property names (or paths) in the target configuration class.
    ///     This format is commonly used when working directly with Microsoft.Extensions.Configuration
    ///     sections or when configuration comes from sources like environment variables or command-line arguments.
    /// </remarks>
    Dictionary,

    /// <summary>
    ///     Indicates that the configuration data is formatted as an XML string.
    /// </summary>
    /// <remarks>
    ///     When this type is used, the configuration data is expected to be a valid XML string
    ///     that can be deserialized into the target configuration class.
    ///     This format is less common in modern applications but is supported for compatibility
    ///     with legacy systems or specific integration scenarios where XML is still used.
    /// </remarks>
    Xml
}