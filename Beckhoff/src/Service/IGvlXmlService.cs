namespace AmlParser.Modular.Service;

public interface IGvlXmlService
{
    /// <summary>
    /// Extracts all variable names from the PLCopen XML and writes them line by line
    /// into a text file.
    /// </summary>
    /// <param name="inputXmlPath">Path to the input XML file.</param>
    /// <param name="outputTxtPath">Path to the output TXT file.</param>
    void GenerateExtractedVariablesTextFromGvlXml(string inputXmlPath, string outputTxtPath);

    /// <summary>
    /// Generates the PlcStatusControl class using XML content and optional
    /// properties-based configuration.
    /// </summary>
    /// <param name="inputXmlPath">Path to the input XML file.</param>
    /// <param name="outputCsPath">Path to the generated C# file.</param>
    /// <param name="propertiesFilePath">Optional path to plcstatus.properties.</param>
    /// <param name="namespace">Optional namespace override for the generated class.</param>
    /// <param name="className">Optional class name for the generated class.</param>
    /// <param name="plcControlTypeName">Type name used for the PLC control dependency field.</param>
    /// <param name="hardwareControlPoolTypeName">Type name used for the constructor parameter.</param>
    /// <param name="plcReadMethodName">Method name used to read PLC node values.</param>
    void GeneratePlcStatusControlFromGvlXml(
        string inputXmlPath,
        string outputCsPath,
        string? propertiesFilePath = null,
        string? @namespace = null,
        string className = "PlcStatusControl",
        string plcControlTypeName = "IPlcControl",
        string hardwareControlPoolTypeName = "IHardwareControlPool",
        string plcReadMethodName = "ReadValueFromPlcNode");
}
