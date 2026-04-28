namespace AmlParser.Modular.Service;

public interface IGvlXmlService
{
    /// <summary>
    /// Creates a temporary XML holder file from an existing PLCopen XML source.
    /// </summary>
    /// <param name="inputXmlPath">Path to the input XML file.</param>
    /// <param name="outputTemplateXmlPath">Path to the generated XML holder file.</param>
    void CreateXmlTemplateHolderFromGvlXml(string inputXmlPath, string outputTemplateXmlPath);

    /// <summary>
    /// Extracts all variable names from the PLCopen XML and writes them line by line
    /// into a text file.
    /// </summary>
    /// <param name="inputXmlPath">Path to the input XML file.</param>
    /// <param name="outputTxtPath">Path to the output TXT file.</param>
    void GenerateExtractedVariablesTextFromGvlXml(string inputXmlPath, string outputTxtPath);

    /// <summary>
    /// Generates the PlcStatusControl class using XML content and optional
    /// CLI-based or properties-based configuration.
    /// </summary>
    /// <param name="inputXmlPath">Path to the input XML file.</param>
    /// <param name="outputCsPath">Path to the generated C# file.</param>
    /// <param name="propertiesFilePath">Optional path to plcstatus.properties.</param>
    /// <param name="cliOverrides">Optional key-value pairs parsed from CLI arguments.</param>
    void GeneratePlcStatusControlFromGvlXml(
        string inputXmlPath,
        string outputCsPath,
        string? propertiesFilePath = null,
        IReadOnlyDictionary<string, string>? cliOverrides = null);
}
