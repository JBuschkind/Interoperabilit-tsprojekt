namespace AmlParser.Modular.Service;

public interface ICSharpToGvlXmlService
{
    /// <summary>
    /// Updates a GVL XML file from variable names extracted from a C# source file.
    /// </summary>
    /// <param name="csharpInputPath">Path to the C# source file.</param>
    /// <param name="templateXmlPath">Path to the XML holder/template file.</param>
    /// <param name="outputXmlPath">Path to the generated XML output file.</param>
    /// <returns>
    /// True when at least one non-constant XML variable name changed; otherwise false.
    /// </returns>
    bool UpdateGvlXmlFromCSharp(
        string csharpInputPath,
        string templateXmlPath,
        string outputXmlPath);
}
