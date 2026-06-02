namespace XmlParser.Modular.Service;

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
    /// Namespace and the two using-lists are configurable; the remaining
    /// generator settings are fixed in <see cref="PlcStatusControlConfig"/>.
    /// </summary>
    /// <param name="inputXmlPath">Path to the input XML file.</param>
    /// <param name="outputGvlCsPath">Path to the generated POCO file.</param>
    /// <param name="outputProxyCsPath">Path to the generated proxy file.</param>
    /// <param name="namespaceName">Optional namespace override for the generated files.</param>
    /// <param name="gvlUsings">Optional extra using-directives for Gvl.cs.</param>
    /// <param name="proxyUsings">Optional extra using-directives for GvlProxy.cs.</param>
    void GenerateFullMappingFromGvlXml(
        string inputXmlPath,
        string outputGvlCsPath,
        string outputProxyCsPath,
        string? namespaceName = null,
        IReadOnlyList<string>? gvlUsings = null,
        IReadOnlyList<string>? proxyUsings = null);
}
