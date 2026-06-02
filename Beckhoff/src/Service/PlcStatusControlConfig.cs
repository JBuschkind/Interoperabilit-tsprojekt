namespace XmlParser.Modular.Service;

/// <summary>
/// Generator settings for the XML -> C# (forward) direction.
/// Namespace and the two using-lists are configurable (via CLI / UI);
/// the hardware control types and read method are fixed for the Beckhoff project.
/// </summary>
internal sealed class PlcStatusControlConfig
{
    /// <summary>Namespace of the generated Gvl.cs and GvlProxy.cs classes.</summary>
    public string Namespace { get; set; } = "OPT.Framework.A200001_Ilca_SensorCalibration.Hardware";

    /// <summary>Extra using-directives added to Gvl.cs (without the 'using' keyword or semicolon).</summary>
    public IReadOnlyList<string> AdditionalUsings { get; set; } = Array.Empty<string>();

    /// <summary>Extra using-directives added to GvlProxy.cs (without the 'using' keyword or semicolon).</summary>
    public IReadOnlyList<string> AdditionalProxyUsings { get; set; } = Array.Empty<string>();

    /// <summary>Using directive always added to GvlProxy.cs for the hardware control types.</summary>
    public string HardwareUsing { get; } = "OPT.Framework.API.HardwareControls";

    /// <summary>Type of the PLC control field held inside each proxy.</summary>
    public string PlcControlTypeName { get; } = "IPlcControl";

    /// <summary>Constructor parameter type that exposes the PLC control.</summary>
    public string HardwareControlPoolTypeName { get; } = "IHardwareControlPool";

    /// <summary>Method on the PLC control used to read a node value.</summary>
    public string PlcReadMethodName { get; } = "ReadValueFromPlcNode";
}
