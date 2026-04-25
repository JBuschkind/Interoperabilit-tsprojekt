namespace AmlParser.Modular.Service;

public interface ICSharpToGvlXmlService
{
    /// <summary>
    /// Updates a GVL XML file from variable names extracted from a C# source file.
    /// </summary>
    /// <param name="csharpInputPath">Path to the C# source file.</param>
    /// <param name="templateXmlPath">Path to the XML holder/template file.</param>
    /// <param name="outputXmlPath">Path to the generated XML output file.</param>
    void UpdateGvlXmlFromCSharp(
        string csharpInputPath,
        string templateXmlPath,
        string outputXmlPath);
}
