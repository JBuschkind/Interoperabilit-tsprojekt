using System.Xml.Linq;

namespace AmlParser.Modular.Service;

public sealed class GvlXmlService : IGvlXmlService
{
    /// <summary>
    /// Reads the PLCopen XML and generates two C# files: a POCO file (Gvl.cs)
    /// mirroring every globalVars block and dataType, plus a proxy file
    /// (GvlProxy.cs) exposing a read method per primitive scalar variable.
    /// </summary>
    public void GenerateFullMappingFromGvlXml(
        string inputXmlPath,
        string outputGvlCsPath,
        string outputProxyCsPath,
        string? propertiesFilePath = null,
        IReadOnlyDictionary<string, string>? cliOverrides = null)
    {
        if (!File.Exists(inputXmlPath))
            throw new FileNotFoundException("Input XML not found", inputXmlPath);

        var doc = XDocument.Load(inputXmlPath, LoadOptions.PreserveWhitespace);
        var model = PlcOpenParser.Parse(doc);
        var settings = PlcStatusControlConfig.Load(propertiesFilePath, cliOverrides);

        var generator = new GvlCodeGenerator(model, settings);

        string gvlCode = generator.EmitGvlFile();
        string proxyCode = generator.EmitProxyFile();

        Directory.CreateDirectory(Path.GetDirectoryName(outputGvlCsPath) ?? ".");
        File.WriteAllText(outputGvlCsPath, gvlCode);

        Directory.CreateDirectory(Path.GetDirectoryName(outputProxyCsPath) ?? ".");
        File.WriteAllText(outputProxyCsPath, proxyCode);
    }

    /// <summary>
    /// Copies the source XML to a holder file preserving the original structure
    /// so reverse translation can later update variable names against it.
    /// </summary>
    public void CreateXmlTemplateHolderFromGvlXml(string inputXmlPath, string outputTemplateXmlPath)
    {
        if (!File.Exists(inputXmlPath))
            throw new FileNotFoundException("Input XML not found", inputXmlPath);

        var doc = XDocument.Load(inputXmlPath, LoadOptions.PreserveWhitespace);
        Directory.CreateDirectory(Path.GetDirectoryName(outputTemplateXmlPath) ?? ".");
        doc.Save(outputTemplateXmlPath);
    }

    /// <summary>
    /// Writes fully qualified variable names (Gvl.Variable) to a text file,
    /// useful for inspecting what the parser sees in the XML.
    /// </summary>
    public void GenerateExtractedVariablesTextFromGvlXml(string inputXmlPath, string outputTxtPath)
    {
        if (!File.Exists(inputXmlPath))
            throw new FileNotFoundException("Input XML not found", inputXmlPath);

        var doc = XDocument.Load(inputXmlPath, LoadOptions.PreserveWhitespace);
        var model = PlcOpenParser.Parse(doc);

        var lines = model.Gvls
            .SelectMany(g => g.Variables.Select(v => $"{g.Name}.{v.Name}"))
            .OrderBy(line => line, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(outputTxtPath) ?? ".");
        File.WriteAllLines(outputTxtPath, lines);
    }
}
