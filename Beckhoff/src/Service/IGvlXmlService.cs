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
    /// Generates a full mapping of all PLCopen globalVars and dataTypes into two
    /// C# files: a POCO file (Gvl.cs) mirroring the PLC structure and a proxy
    /// file (GvlProxy.cs) with read methods per primitive variable.
    /// </summary>
    /// <param name="inputXmlPath">Path to the input XML file.</param>
    /// <param name="outputGvlCsPath">Path to the generated POCO file.</param>
    /// <param name="outputProxyCsPath">Path to the generated proxy file.</param>
    /// <param name="propertiesFilePath">Optional path to plcstatus.properties.</param>
    /// <param name="cliOverrides">Optional key-value pairs parsed from CLI arguments.</param>
    void GenerateFullMappingFromGvlXml(
        string inputXmlPath,
        string outputGvlCsPath,
        string outputProxyCsPath,
        string? propertiesFilePath = null,
        IReadOnlyDictionary<string, string>? cliOverrides = null);
}
