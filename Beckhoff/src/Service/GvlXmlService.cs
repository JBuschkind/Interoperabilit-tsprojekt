using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AmlParser.Modular.Service;

public sealed class GvlXmlService : IGvlXmlService
{
    private static readonly XNamespace PlcOpenNs = "http://www.plcopen.org/xml/tc6_0200";

    /// <summary>
    /// Reads the PLCopen XML, resolves relevant PLC nodes, and generates
    /// the target PlcStatusControl class including method bodies.
    /// </summary>
    /// <param name="inputXmlPath">Path to the input XML file.</param>
    /// <param name="outputCsPath">Path to the generated C# file.</param>
    /// <param name="propertiesFilePath">Optional path to a properties file with overrides.</param>
    /// <param name="namespace">Optional namespace override for the generated class.</param>
    /// <param name="className">Optional class name for the generated class.</param>
    /// <param name="plcControlTypeName">Type name for the PLC control field.</param>
    /// <param name="hardwareControlPoolTypeName">Type name for the constructor parameter.</param>
    /// <param name="plcReadMethodName">Method name used for PLC node reads.</param>
    public void GeneratePlcStatusControlFromGvlXml(
        string inputXmlPath,
        string outputCsPath,
        string? propertiesFilePath = null,
        string? @namespace = null,
        string className = "PlcStatusControl",
        string plcControlTypeName = "IPlcControl",
        string hardwareControlPoolTypeName = "IHardwareControlPool",
        string plcReadMethodName = "ReadValueFromPlcNode")
    {
        if (!File.Exists(inputXmlPath))
            throw new FileNotFoundException("Input XML not found", inputXmlPath);

        var doc = XDocument.Load(inputXmlPath, LoadOptions.PreserveWhitespace);
        var allVars = ReadAllVarsFromPlcOpenXml(doc)
            .GroupBy(v => (v.GvlName, v.Name), StringTupleComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(v => v.IsConst).First())
            .Where(v => !v.IsConst)
            .ToList();

        var variableLookup = allVars
            .GroupBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var settings = PlcStatusControlConfig.Load(
            inputXmlPath,
            propertiesFilePath,
            @namespace,
            className,
            plcControlTypeName,
            hardwareControlPoolTypeName,
            plcReadMethodName);

        string? plcSystemStateNode = ResolveNodePath(
            settings.PlcSystemStateNode,
            variableLookup,
            "SystemStatus",
            "PlcSystemState");

        string? allPlcNodesPresentNode = ResolveNodePath(
            settings.AllPlcNodesPresentNode,
            variableLookup,
            "AllPartitiantsPresent",
            "AllParticipantsPresent",
            "AllPlcNodesPresentState");

        string? canOpenStateNode = ResolveNodePath(
            settings.CanOpenStateNode,
            variableLookup,
            "CANOpenState",
            "CanOpenState");

        string? appTimestampNode = ResolveNodePath(
            settings.AppTimestampNode,
            variableLookup,
            "AppTimestamp");

        string? appVersionNode = ResolveNodePath(
            settings.AppVersionNode,
            variableLookup,
            "AppVersion");

        string plcSystemStateSourceType = ResolvePlcSystemStateSourceType(
            settings.PlcSystemStateSourceType,
            variableLookup);

        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        if (!string.IsNullOrWhiteSpace(settings.EnumUsing))
            sb.AppendLine($"using {settings.EnumUsing};");
        if (!string.IsNullOrWhiteSpace(settings.HardwareUsing))
            sb.AppendLine($"using {settings.HardwareUsing};");
        sb.AppendLine();

        bool hasNamespace = !string.IsNullOrWhiteSpace(settings.Namespace);
        if (hasNamespace)
        {
            sb.AppendLine($"namespace {settings.Namespace}");
            sb.AppendLine("{");
        }

        string indent = hasNamespace ? "    " : string.Empty;
        string generatedClassName = SanitizeIdentifier(settings.ClassName);

        sb.AppendLine($"{indent}/// <inheritdoc />");
        sb.AppendLine($"{indent}public class {generatedClassName} : {settings.InterfaceName}");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    private readonly {settings.PlcControlTypeName} _plcControl;");
        sb.AppendLine();
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// Initializes a new instance of the class.");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    public {generatedClassName}({settings.HardwareControlPoolTypeName} hardwareControl)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        _plcControl = hardwareControl.PlcControl;");
        sb.AppendLine($"{indent}    }}");

        AppendPlcSystemStateMethod(sb, indent, settings, plcSystemStateNode, plcSystemStateSourceType);
        AppendBoolMethod(sb, indent, settings, "GetAllPlcNodesPresentState", allPlcNodesPresentNode);
        AppendBoolMethod(sb, indent, settings, "GetCanOpenState", canOpenStateNode);
        AppendCompileTimeMethod(sb, indent, settings, appTimestampNode);
        AppendStringMethod(sb, indent, settings, "GetAppVersion", appVersionNode);

        sb.AppendLine($"{indent}}}");

        if (hasNamespace)
            sb.AppendLine("}");

        Directory.CreateDirectory(Path.GetDirectoryName(outputCsPath) ?? ".");
        File.WriteAllText(outputCsPath, sb.ToString());
    }

    /// <summary>
    /// Extracts all variables from the XML, normalizes duplicates,
    /// and writes fully qualified names into a TXT file.
    /// </summary>
    /// <param name="inputXmlPath">Path to the input XML file.</param>
    /// <param name="outputTxtPath">Path to the output TXT file.</param>
    public void GenerateExtractedVariablesTextFromGvlXml(string inputXmlPath, string outputTxtPath)
    {
        if (!File.Exists(inputXmlPath))
            throw new FileNotFoundException("Input XML not found", inputXmlPath);

        var doc = XDocument.Load(inputXmlPath, LoadOptions.PreserveWhitespace);
        var variableLines = ReadAllVarsFromPlcOpenXml(doc)
            .GroupBy(v => (v.GvlName, v.Name), StringTupleComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(v => v.IsConst).First())
            .OrderBy(v => v.GvlName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .Select(v => $"{v.GvlName}.{v.Name}")
            .ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(outputTxtPath) ?? ".");
        File.WriteAllLines(outputTxtPath, variableLines);
    }

    /// <summary>
    /// Reads all globalVars blocks from the PLCopen document and maps each variable
    /// to an internal model used by later generation steps.
    /// </summary>
    /// <param name="doc">Already loaded PLCopen XML document.</param>
    /// <returns>List of all discovered variables including metadata.</returns>
    private static List<GvlVar> ReadAllVarsFromPlcOpenXml(XDocument doc)
    {
        var globalVarsBlocks = doc
            .Descendants(PlcOpenNs + "globalVars")
            .ToList();

        if (globalVarsBlocks.Count == 0)
            return new List<GvlVar>();

        string fallbackGvlName = globalVarsBlocks
            .Select(g => (string?)g.Attribute("name"))
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
            ?? "GlobalVariables";

        var allVars = new List<GvlVar>();

        foreach (var gvl in globalVarsBlocks)
        {
            string gvlName = (string?)gvl.Attribute("name") ?? fallbackGvlName;
            bool isConstantList = ParseBool((string?)gvl.Attribute("constant"));

            foreach (var variable in gvl.Elements(PlcOpenNs + "variable"))
            {
                string? name = (string?)variable.Attribute("name");
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                string csType = MapPlcToCSharpType(ParsePlcType(variable));

                allVars.Add(new GvlVar(
                    Name: name,
                    GvlName: gvlName,
                    IsConst: isConstantList,
                    CsType: csType));
            }
        }

        return allVars;
    }

    /// <summary>
    /// Writes the GetPlcSystemState method into the generated target class.
    /// The method is generated either with node access or as NotImplemented.
    /// </summary>
    /// <param name="sb">StringBuilder for the output file.</param>
    /// <param name="indent">Indentation for the current namespace context.</param>
    /// <param name="settings">Active generator configuration.</param>
    /// <param name="nodePath">Resolved PLC node for the state value.</param>
    /// <param name="sourceType">Source type used by the ReadValue call.</param>
    private static void AppendPlcSystemStateMethod(
        StringBuilder sb,
        string indent,
        PlcStatusControlConfig settings,
        string? nodePath,
        string sourceType)
    {
        sb.AppendLine();
        sb.AppendLine($"{indent}    /// <inheritdoc />");
        sb.AppendLine($"{indent}    public {settings.EnumTypeName} GetPlcSystemState()");
        sb.AppendLine($"{indent}    {{");

        if (string.IsNullOrWhiteSpace(nodePath))
        {
            sb.AppendLine($"{indent}        throw new NotImplementedException();");
        }
        else
        {
            sb.AppendLine($"{indent}        var result = _plcControl.{settings.PlcReadMethodName}<{sourceType}>(\"{EscapeString(nodePath)}\");");
            sb.AppendLine($"{indent}        return result.IsSuccess ? ({settings.EnumTypeName})result.Value : default;");
        }

        sb.AppendLine($"{indent}    }}");
    }

    /// <summary>
    /// Writes a boolean getter method with optional PLC node resolution
    /// into the generated class.
    /// </summary>
    /// <param name="sb">StringBuilder for the output file.</param>
    /// <param name="indent">Indentation for the current namespace context.</param>
    /// <param name="settings">Active generator configuration.</param>
    /// <param name="methodName">Name of the method to generate.</param>
    /// <param name="nodePath">PLC node for the boolean value.</param>
    private static void AppendBoolMethod(
        StringBuilder sb,
        string indent,
        PlcStatusControlConfig settings,
        string methodName,
        string? nodePath)
    {
        sb.AppendLine();
        sb.AppendLine($"{indent}    /// <inheritdoc />");
        sb.AppendLine($"{indent}    public bool {methodName}()");
        sb.AppendLine($"{indent}    {{");

        if (string.IsNullOrWhiteSpace(nodePath))
        {
            sb.AppendLine($"{indent}        throw new NotImplementedException();");
        }
        else
        {
            sb.AppendLine($"{indent}        var result = _plcControl.{settings.PlcReadMethodName}<bool>(\"{EscapeString(nodePath)}\");");
            sb.AppendLine($"{indent}        return (result.IsSuccess && result.Value);");
        }

        sb.AppendLine($"{indent}    }}");
    }

    /// <summary>
    /// Writes the GetCompileTime method into the generated class and computes
    /// the time difference between now and the PLC timestamp.
    /// </summary>
    /// <param name="sb">StringBuilder for the output file.</param>
    /// <param name="indent">Indentation for the current namespace context.</param>
    /// <param name="settings">Active generator configuration.</param>
    /// <param name="nodePath">PLC node for the compile timestamp.</param>
    private static void AppendCompileTimeMethod(
        StringBuilder sb,
        string indent,
        PlcStatusControlConfig settings,
        string? nodePath)
    {
        sb.AppendLine();
        sb.AppendLine($"{indent}    /// <inheritdoc />");
        sb.AppendLine($"{indent}    public TimeSpan GetCompileTime()");
        sb.AppendLine($"{indent}    {{");

        if (string.IsNullOrWhiteSpace(nodePath))
        {
            sb.AppendLine($"{indent}        throw new NotImplementedException();");
        }
        else
        {
            sb.AppendLine($"{indent}        var result = _plcControl.{settings.PlcReadMethodName}<DateTime>(\"{EscapeString(nodePath)}\");");
            sb.AppendLine($"{indent}        return result.IsSuccess ? DateTime.Now - result.Value : TimeSpan.Zero;");
        }

        sb.AppendLine($"{indent}    }}");
    }

    /// <summary>
    /// Writes a string-based getter method into the generated class.
    /// If no node is available, a NotImplementedException is emitted.
    /// </summary>
    /// <param name="sb">StringBuilder for the output file.</param>
    /// <param name="indent">Indentation for the current namespace context.</param>
    /// <param name="settings">Active generator configuration.</param>
    /// <param name="methodName">Name of the method to generate.</param>
    /// <param name="nodePath">PLC node for the string value.</param>
    private static void AppendStringMethod(
        StringBuilder sb,
        string indent,
        PlcStatusControlConfig settings,
        string methodName,
        string? nodePath)
    {
        sb.AppendLine();
        sb.AppendLine($"{indent}    /// <inheritdoc />");
        sb.AppendLine($"{indent}    public string {methodName}()");
        sb.AppendLine($"{indent}    {{");

        if (string.IsNullOrWhiteSpace(nodePath))
        {
            sb.AppendLine($"{indent}        throw new NotImplementedException();");
        }
        else
        {
            sb.AppendLine($"{indent}        var result = _plcControl.{settings.PlcReadMethodName}<string>(\"{EscapeString(nodePath)}\");");
            sb.AppendLine($"{indent}        return result.Value;");
        }

        sb.AppendLine($"{indent}    }}");
    }

    /// <summary>
    /// Determines the effective PLC node to use. A configured node has priority;
    /// otherwise candidate names are resolved against extracted variables.
    /// </summary>
    /// <param name="configuredNode">Explicit node from configuration.</param>
    /// <param name="variableLookup">Lookup of extracted variables by name.</param>
    /// <param name="variableCandidates">Fallback candidate names from XML.</param>
    /// <returns>Resolved fully qualified node or null.</returns>
    private static string? ResolveNodePath(
        string? configuredNode,
        IReadOnlyDictionary<string, GvlVar> variableLookup,
        params string[] variableCandidates)
    {
        if (!string.IsNullOrWhiteSpace(configuredNode))
            return configuredNode;

        foreach (var variableName in variableCandidates)
        {
            if (!variableLookup.TryGetValue(variableName, out var variable))
                continue;

            string gvlName = string.IsNullOrWhiteSpace(variable.GvlName)
                ? "GlobalVariables"
                : variable.GvlName;

            return $"{gvlName}.{variable.Name}";
        }

        return null;
    }

    /// <summary>
    /// Resolves the source type used for system state reads. It first uses an explicit
    /// configured type and then falls back to an automatically detected type.
    /// </summary>
    /// <param name="configuredSourceType">Explicit source type from configuration.</param>
    /// <param name="variableLookup">Lookup of extracted variables.</param>
    /// <returns>A valid C# type name for the ReadValue call.</returns>
    private static string ResolvePlcSystemStateSourceType(
        string? configuredSourceType,
        IReadOnlyDictionary<string, GvlVar> variableLookup)
    {
        if (!string.IsNullOrWhiteSpace(configuredSourceType))
            return configuredSourceType;

        if (variableLookup.TryGetValue("SystemStatus", out var systemStatus)
            && IsNumericType(systemStatus.CsType))
        {
            return systemStatus.CsType;
        }

        if (variableLookup.TryGetValue("PlcSystemState", out var plcState)
            && IsNumericType(plcState.CsType))
        {
            return plcState.CsType;
        }

        return "ushort";
    }

    /// <summary>
    /// Checks whether a C# type is a numeric primitive type.
    /// </summary>
    /// <param name="csType">Type name to validate.</param>
    /// <returns>True for numeric types; otherwise false.</returns>
    private static bool IsNumericType(string csType)
    {
        return csType is "byte"
            or "sbyte"
            or "short"
            or "ushort"
            or "int"
            or "uint"
            or "long"
            or "ulong";
    }

            /// <summary>
            /// Extracts the PLC data type of a variable from its type node.
            /// </summary>
            /// <param name="variable">XML element representing a PLC variable.</param>
            /// <returns>PLC type name, or an empty string when not present.</returns>
    private static string ParsePlcType(XElement variable)
    {
        var typeElement = variable.Element(PlcOpenNs + "type");
        if (typeElement == null)
            return string.Empty;

        var concrete = typeElement.Elements().FirstOrDefault();
        if (concrete == null)
            return string.Empty;

        return concrete.Name.LocalName;
    }

    /// <summary>
    /// Maps PLC data types to C# type names used by the generator.
    /// </summary>
    /// <param name="plcType">PLC type name from XML.</param>
    /// <returns>Corresponding C# type name.</returns>
    private static string MapPlcToCSharpType(string plcType)
    {
        return plcType.ToUpperInvariant() switch
        {
            "BOOL" => "bool",
            "BYTE" => "byte",
            "USINT" => "byte",
            "SINT" => "sbyte",
            "UINT" => "ushort",
            "INT" => "short",
            "UDINT" => "uint",
            "DINT" => "int",
            "ULINT" => "ulong",
            "LINT" => "long",
            "REAL" => "float",
            "LREAL" => "double",
            "STRING" or "STRING1" => "string",
            "WSTRING" => "string",
            "DT" => "DateTime",
            "DATE" => "DateOnly",
            "TIME" => "TimeSpan",
            _ => "string"
        };
    }

    /// <summary>
    /// Converts arbitrary names into valid C# identifiers.
    /// </summary>
    /// <param name="raw">Raw value from configuration or XML.</param>
    /// <returns>Valid identifier for generated C# code.</returns>
    private static string SanitizeIdentifier(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "Unnamed";

        string name = Regex.Replace(raw, "[^A-Za-z0-9_]", "_");
        if (string.IsNullOrEmpty(name))
            return "Unnamed";

        if (char.IsDigit(name[0]))
            name = "_" + name;

        return name;
    }

    /// <summary>
    /// Escapes backslashes and quotes for use in C# string literals.
    /// </summary>
    /// <param name="value">Unescaped source string.</param>
    /// <returns>String with escaped characters suitable for C# literals.</returns>
    private static string EscapeString(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>
    /// Parses boolean values from XML attributes where "true" and "1" are treated as true.
    /// </summary>
    /// <param name="value">Text value from XML attributes or configuration.</param>
    /// <returns>True for recognized truthy values; otherwise false.</returns>
    private static bool ParseBool(string? value)
        => value != null && (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1"));

    private readonly record struct GvlVar(
        string Name,
        string GvlName,
        bool IsConst,
        string CsType
    );

    private sealed class StringTupleComparer : IEqualityComparer<(string A, string B)>
    {
        public static readonly StringTupleComparer OrdinalIgnoreCase = new(StringComparer.OrdinalIgnoreCase);

        private readonly StringComparer _comparer;

        /// <summary>
        /// Initializes the comparer with the requested string comparison strategy.
        /// </summary>
        /// <param name="comparer">Comparer used for table and variable names.</param>
        private StringTupleComparer(StringComparer comparer)
        {
            _comparer = comparer;
        }

        /// <summary>
        /// Compares two tuples of table and variable names using the configured
        /// string comparison strategy.
        /// </summary>
        /// <param name="x">First tuple.</param>
        /// <param name="y">Second tuple.</param>
        /// <returns>True when both tuples are considered equal.</returns>
        public bool Equals((string A, string B) x, (string A, string B) y)
            => _comparer.Equals(x.A, y.A) && _comparer.Equals(x.B, y.B);

        /// <summary>
        /// Computes a hash code for a tuple using the configured comparer.
        /// </summary>
        /// <param name="obj">Tuple containing table and variable name.</param>
        /// <returns>Combined hash code for dictionary and grouping operations.</returns>
        public int GetHashCode((string A, string B) obj)
            => HashCode.Combine(_comparer.GetHashCode(obj.A), _comparer.GetHashCode(obj.B));
    }
}
