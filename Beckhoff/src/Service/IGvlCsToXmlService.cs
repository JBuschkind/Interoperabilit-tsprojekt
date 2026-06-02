namespace XmlParser.Modular.Service;

public interface IGvlCsToXmlService
{
    /// <summary>
    /// Generates a fresh PLCopen XML file from a Gvl.cs (POCO mirror)
    /// and an optional GvlProxy.cs that provides typed node paths.
    /// No XML template is required; defaults are used for headers, tasks and ObjectIds.
    /// </summary>
    /// <param name="gvlCsPath">Path to the Gvl.cs file with GVL and dataType classes.</param>
    /// <param name="gvlProxyCsPath">Optional path to GvlProxy.cs for type confirmation.</param>
    /// <param name="outputXmlPath">Path to the generated PLCopen XML file.</param>
    void GenerateXmlFromCSharp(
        string gvlCsPath,
        string? gvlProxyCsPath,
        string outputXmlPath);
}
