namespace AmlParser.Modular.Service;

internal sealed class PlcStatusControlConfig
{
    private static readonly string[] KnownEnvironmentKeys =
    {
        "PLCSTATUS_NAMESPACE",
        "PLCSTATUS_ENUM_USING",
        "PLCSTATUS_HARDWARE_USING",
        "PLCSTATUS_CLASS_NAME",
        "PLCSTATUS_INTERFACE_NAME",
        "PLCSTATUS_PLC_CONTROL_TYPE_NAME",
        "PLCSTATUS_HARDWARE_CONTROL_POOL_TYPE_NAME",
        "PLCSTATUS_PLC_READ_METHOD_NAME",
        "PLCSTATUS_ENUM_TYPE_NAME",
        "PLCSTATUS_PLC_SYSTEM_STATE_SOURCE_TYPE",
        "PLCSTATUS_PLC_SYSTEM_STATE_NODE",
        "PLCSTATUS_ALL_PLC_NODES_PRESENT_NODE",
        "PLCSTATUS_CAN_OPEN_STATE_NODE",
        "PLCSTATUS_APP_TIMESTAMP_NODE",
        "PLCSTATUS_APP_VERSION_NODE"
    };

    public string Namespace { get; private set; } = "OPT.Framework.A200001_Ilca_SensorCalibration.Hardware";
    public string EnumUsing { get; private set; } = "OPT.Framework.A200001_Ilca_SensorCalibration.Enumerations";
    public string HardwareUsing { get; private set; } = "OPT.Framework.API.HardwareControls";
    public string ClassName { get; private set; } = "PlcStatusControl";
    public string InterfaceName { get; private set; } = "IPlcStatusControl";
    public string PlcControlTypeName { get; private set; } = "IPlcControl";
    public string HardwareControlPoolTypeName { get; private set; } = "IHardwareControlPool";
    public string PlcReadMethodName { get; private set; } = "ReadValueFromPlcNode";
    public string EnumTypeName { get; private set; } = "EPlcState";
    public string PlcSystemStateSourceType { get; private set; } = "ushort";

    public string? PlcSystemStateNode { get; private set; }
    public string? AllPlcNodesPresentNode { get; private set; }
    public string? CanOpenStateNode { get; private set; }
    public string? AppTimestampNode { get; private set; }
    public string? AppVersionNode { get; private set; }

    /// <summary>
    /// Creates a configuration instance with defaults and then applies
    /// properties and environment overrides in a defined order.
    /// </summary>
    /// <param name="inputXmlPath">Input XML path used to derive the default properties file location.</param>
    /// <param name="propertiesFilePath">Optional explicit path to a properties file.</param>
    /// <param name="namespaceOverride">Optional namespace override.</param>
    /// <param name="className">Optional class name override.</param>
    /// <param name="plcControlTypeName">Optional type name for PLC control.</param>
    /// <param name="hardwareControlPoolTypeName">Optional type name for the hardware pool.</param>
    /// <param name="plcReadMethodName">Optional method name used for PLC reads.</param>
    /// <returns>Fully resolved generator configuration.</returns>
    public static PlcStatusControlConfig Load(
        string inputXmlPath,
        string? propertiesFilePath,
        string? namespaceOverride,
        string className,
        string plcControlTypeName,
        string hardwareControlPoolTypeName,
        string plcReadMethodName)
    {
        var config = new PlcStatusControlConfig();

        if (!string.IsNullOrWhiteSpace(namespaceOverride))
            config.Namespace = namespaceOverride;

        if (!string.IsNullOrWhiteSpace(className))
            config.ClassName = className;

        if (!string.IsNullOrWhiteSpace(plcControlTypeName))
            config.PlcControlTypeName = plcControlTypeName;

        if (!string.IsNullOrWhiteSpace(hardwareControlPoolTypeName))
            config.HardwareControlPoolTypeName = hardwareControlPoolTypeName;

        if (!string.IsNullOrWhiteSpace(plcReadMethodName))
            config.PlcReadMethodName = plcReadMethodName;

        string inputFolder = Path.GetDirectoryName(inputXmlPath) ?? ".";
        string defaultPropertiesPath = Path.Combine(inputFolder, "plcstatus.properties");
        string effectivePropertiesPath = string.IsNullOrWhiteSpace(propertiesFilePath)
            ? defaultPropertiesPath
            : propertiesFilePath;

        if (File.Exists(effectivePropertiesPath))
            config.ApplyValues(ReadPropertiesFile(effectivePropertiesPath));

        config.ApplyValues(ReadEnvironmentValues());
        return config;
    }

    /// <summary>
    /// Applies a key-value set to the current configuration.
    /// Keys are normalized and then mapped to known fields.
    /// </summary>
    /// <param name="values">Input values from properties or environment variables.</param>
    private void ApplyValues(IReadOnlyDictionary<string, string> values)
    {
        foreach (var entry in values)
        {
            string normalizedKey = NormalizeKey(entry.Key);
            string value = entry.Value.Trim();

            if (string.IsNullOrWhiteSpace(value))
                continue;

            switch (normalizedKey)
            {
                case "namespace":
                    Namespace = value;
                    break;
                case "enumusing":
                    EnumUsing = value;
                    break;
                case "hardwareusing":
                    HardwareUsing = value;
                    break;
                case "classname":
                    ClassName = value;
                    break;
                case "interfacename":
                    InterfaceName = value;
                    break;
                case "plccontroltypename":
                    PlcControlTypeName = value;
                    break;
                case "hardwarecontrolpooltypename":
                    HardwareControlPoolTypeName = value;
                    break;
                case "plcreadmethodname":
                    PlcReadMethodName = value;
                    break;
                case "enumtypename":
                    EnumTypeName = value;
                    break;
                case "plcsystemstatesourcetype":
                    PlcSystemStateSourceType = value;
                    break;
                case "plcsystemstatenode":
                    PlcSystemStateNode = value;
                    break;
                case "allplcnodespresentnode":
                    AllPlcNodesPresentNode = value;
                    break;
                case "canopenstatenode":
                    CanOpenStateNode = value;
                    break;
                case "apptimestampnode":
                    AppTimestampNode = value;
                    break;
                case "appversionnode":
                    AppVersionNode = value;
                    break;
            }
        }
    }

    /// <summary>
    /// Normalizes keys by removing separators, forcing lowercase,
    /// and stripping an optional "plcstatus" prefix.
    /// </summary>
    /// <param name="key">Original key from file or environment.</param>
    /// <returns>Normalized comparison key.</returns>
    private static string NormalizeKey(string key)
    {
        string normalized = key.Trim()
            .Replace(".", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .ToLowerInvariant();

        if (normalized.StartsWith("plcstatus", StringComparison.Ordinal))
            normalized = normalized["plcstatus".Length..];

        return normalized;
    }

    /// <summary>
    /// Reads known configuration values from environment variables with the PLCSTATUS_ prefix.
    /// </summary>
    /// <returns>Dictionary containing discovered environment values.</returns>
    private static Dictionary<string, string> ReadEnvironmentValues()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string key in KnownEnvironmentKeys)
        {
            string? value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
                values[key] = value;
        }

        return values;
    }

    /// <summary>
    /// Reads a Java-properties-like file and extracts valid key-value pairs,
    /// skipping comment and empty lines.
    /// </summary>
    /// <param name="filePath">Path to the properties file.</param>
    /// <returns>Dictionary with loaded configuration values.</returns>
    private static Dictionary<string, string> ReadPropertiesFile(string filePath)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in File.ReadLines(filePath))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                continue;

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            string key = line[..separatorIndex].Trim();
            string value = line[(separatorIndex + 1)..].Trim();
            values[key] = value;
        }

        return values;
    }
}
