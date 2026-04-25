using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Text;

namespace AmlParser.Modular.Service;

public sealed class CSharpToGvlXmlService : ICSharpToGvlXmlService
{
    private static readonly Regex ReadCallNodePathRegex = new(
        "ReadValueFromPlcNode\\s*<\\s*(?<type>[^>]+)\\s*>\\s*\\(\\s*\"(?<gvl>[A-Za-z_][A-Za-z0-9_]*)\\.(?<name>[A-Za-z_][A-Za-z0-9_]*)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex GvlNodePathRegex = new(
        "\"(?<gvl>[A-Za-z_][A-Za-z0-9_]*)\\.(?<name>[A-Za-z_][A-Za-z0-9_]*)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Reads a C# source file, extracts GVL variable names from node paths,
    /// and writes these names back into an XML holder/template file.
    /// </summary>
    /// <param name="csharpInputPath">Path to the C# source file.</param>
    /// <param name="templateXmlPath">Path to the XML holder/template file.</param>
    /// <param name="outputXmlPath">Path to the generated XML output file.</param>
    public void UpdateGvlXmlFromCSharp(
        string csharpInputPath,
        string templateXmlPath,
        string outputXmlPath)
    {
        if (!File.Exists(csharpInputPath))
            throw new FileNotFoundException("C# input file not found", csharpInputPath);

        if (!File.Exists(templateXmlPath))
            throw new FileNotFoundException("XML template file not found", templateXmlPath);

        string csharpContent = File.ReadAllText(csharpInputPath);
        var variableSpecsByGvl = ExtractVariableSpecsByGvl(csharpContent);

        var doc = XDocument.Load(templateXmlPath, LoadOptions.PreserveWhitespace);
        ApplyVariableSpecsToXml(doc, variableSpecsByGvl);

        Directory.CreateDirectory(Path.GetDirectoryName(outputXmlPath) ?? ".");
        doc.Save(outputXmlPath);
    }

    /// <summary>
    /// Extracts unique GVL variable specifications from C# source content.
    /// It first reads typed ReadValue calls and then falls back to generic node-path strings.
    /// </summary>
    /// <param name="csharpContent">Full C# source content.</param>
    /// <returns>Dictionary from GVL name to ordered variable specifications.</returns>
    private static IReadOnlyDictionary<string, IReadOnlyList<GvlVariableSpec>> ExtractVariableSpecsByGvl(string csharpContent)
    {
        var extractedNodes = new List<GvlVariableSpec>();
        var seen = new HashSet<(string Gvl, string Name)>(NodeTupleComparer.OrdinalIgnoreCase);

        foreach (Match match in ReadCallNodePathRegex.Matches(csharpContent))
        {
            string gvlName = match.Groups["gvl"].Value;
            string variableName = match.Groups["name"].Value;
            string? csharpType = NormalizeCSharpTypeName(match.Groups["type"].Value);

            if (string.IsNullOrWhiteSpace(gvlName) || string.IsNullOrWhiteSpace(variableName))
                continue;

            if (seen.Add((gvlName, variableName)))
                extractedNodes.Add(new GvlVariableSpec(gvlName, variableName, csharpType));
        }

        foreach (Match match in GvlNodePathRegex.Matches(csharpContent))
        {
            string gvlName = match.Groups["gvl"].Value;
            string variableName = match.Groups["name"].Value;

            if (string.IsNullOrWhiteSpace(gvlName) || string.IsNullOrWhiteSpace(variableName))
                continue;

            if (seen.Add((gvlName, variableName)))
                extractedNodes.Add(new GvlVariableSpec(gvlName, variableName, null));
        }

        if (extractedNodes.Count == 0)
            throw new InvalidOperationException("No GVL node paths were found in the provided C# file.");

        return extractedNodes
            .GroupBy(node => node.GvlName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<GvlVariableSpec>)group.ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Applies extracted variable specifications to all matching non-constant globalVars blocks.
    /// Existing unmatched XML variables are preserved, and new variables from C# are inserted.
    /// </summary>
    /// <param name="doc">Loaded XML document to update.</param>
    /// <param name="variableSpecsByGvl">Variable specifications grouped by GVL name.</param>
    private static void ApplyVariableSpecsToXml(
        XDocument doc,
        IReadOnlyDictionary<string, IReadOnlyList<GvlVariableSpec>> variableSpecsByGvl)
    {
        XNamespace plcOpenNs = doc.Root?.Name.Namespace ?? "http://www.plcopen.org/xml/tc6_0200";
        var renameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var insertedSpecs = new List<GvlVariableSpec>();

        foreach (var globalVars in doc.Descendants(plcOpenNs + "globalVars"))
        {
            if (ParseBool((string?)globalVars.Attribute("constant")))
                continue;

            string gvlName = (string?)globalVars.Attribute("name") ?? "GlobalVariables";
            if (!variableSpecsByGvl.TryGetValue(gvlName, out var desiredSpecs) || desiredSpecs.Count == 0)
                continue;

            var existingVariables = globalVars.Elements(plcOpenNs + "variable").ToList();
            var rebuiltVariables = RebuildVariableElements(
                existingVariables,
                desiredSpecs,
                plcOpenNs,
                renameMap,
                insertedSpecs);

            globalVars.Elements(plcOpenNs + "variable").Remove();
            foreach (var variable in rebuiltVariables)
                globalVars.Add(variable);
        }

        ApplyPlainTextRenames(doc, renameMap);
        ApplyPlainTextInsertions(doc, insertedSpecs);
    }

    /// <summary>
    /// Rebuilds a variable list by aligning existing XML variables with desired C# variables.
    /// It preserves unmatched old variables and inserts missing new variables.
    /// </summary>
    /// <param name="existingVariables">Current XML variable elements.</param>
    /// <param name="desiredSpecs">Desired variables extracted from C#.</param>
    /// <param name="plcOpenNs">PLCopen XML namespace.</param>
    /// <param name="renameMap">Map collecting old-to-new renames.</param>
    /// <param name="insertedSpecs">Collection of newly inserted specs.</param>
    /// <returns>Rebuilt variable list.</returns>
    private static List<XElement> RebuildVariableElements(
        IReadOnlyList<XElement> existingVariables,
        IReadOnlyList<GvlVariableSpec> desiredSpecs,
        XNamespace plcOpenNs,
        IDictionary<string, string> renameMap,
        IList<GvlVariableSpec> insertedSpecs)
    {
        var rebuilt = new List<XElement>();
        int oldIndex = 0;
        int newIndex = 0;

        while (oldIndex < existingVariables.Count && newIndex < desiredSpecs.Count)
        {
            XElement currentExisting = existingVariables[oldIndex];
            string currentExistingName = (string?)currentExisting.Attribute("name") ?? string.Empty;
            GvlVariableSpec currentDesired = desiredSpecs[newIndex];

            if (string.Equals(currentExistingName, currentDesired.VariableName, StringComparison.OrdinalIgnoreCase))
            {
                rebuilt.Add(CloneVariableWithNameAndType(currentExisting, currentDesired.VariableName, currentDesired.CSharpType, plcOpenNs));
                oldIndex++;
                newIndex++;
                continue;
            }

            bool desiredExistsLaterInOld = ContainsExistingNameFromIndex(existingVariables, oldIndex + 1, currentDesired.VariableName);
            bool oldExistsLaterInNew = ContainsDesiredNameFromIndex(desiredSpecs, newIndex + 1, currentExistingName);

            if (!desiredExistsLaterInOld && !oldExistsLaterInNew)
            {
                rebuilt.Add(CloneVariableWithNameAndType(currentExisting, currentDesired.VariableName, currentDesired.CSharpType, plcOpenNs));

                if (!string.IsNullOrWhiteSpace(currentExistingName) && !renameMap.ContainsKey(currentExistingName))
                    renameMap[currentExistingName] = currentDesired.VariableName;

                oldIndex++;
                newIndex++;
                continue;
            }

            if (!desiredExistsLaterInOld && oldExistsLaterInNew)
            {
                rebuilt.Add(CreateVariableFromTemplate(existingVariables, oldIndex, currentDesired, plcOpenNs));
                insertedSpecs.Add(currentDesired);
                newIndex++;
                continue;
            }

            rebuilt.Add(new XElement(currentExisting));
            oldIndex++;
        }

        while (oldIndex < existingVariables.Count)
        {
            rebuilt.Add(new XElement(existingVariables[oldIndex]));
            oldIndex++;
        }

        while (newIndex < desiredSpecs.Count)
        {
            GvlVariableSpec remaining = desiredSpecs[newIndex];
            rebuilt.Add(CreateVariableFromTemplate(existingVariables, existingVariables.Count - 1, remaining, plcOpenNs));
            insertedSpecs.Add(remaining);
            newIndex++;
        }

        return rebuilt;
    }

    /// <summary>
    /// Checks whether an existing XML variable name appears later in the list.
    /// </summary>
    /// <param name="existingVariables">Existing XML variable elements.</param>
    /// <param name="startIndex">Start index for search.</param>
    /// <param name="name">Variable name to search for.</param>
    /// <returns>True if the name appears later; otherwise false.</returns>
    private static bool ContainsExistingNameFromIndex(IReadOnlyList<XElement> existingVariables, int startIndex, string name)
    {
        for (int index = startIndex; index < existingVariables.Count; index++)
        {
            string current = (string?)existingVariables[index].Attribute("name") ?? string.Empty;
            if (string.Equals(current, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether a desired C# variable name appears later in the list.
    /// </summary>
    /// <param name="desiredSpecs">Desired variable specifications.</param>
    /// <param name="startIndex">Start index for search.</param>
    /// <param name="name">Variable name to search for.</param>
    /// <returns>True if the name appears later; otherwise false.</returns>
    private static bool ContainsDesiredNameFromIndex(IReadOnlyList<GvlVariableSpec> desiredSpecs, int startIndex, string name)
    {
        for (int index = startIndex; index < desiredSpecs.Count; index++)
        {
            if (string.Equals(desiredSpecs[index].VariableName, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a new XML variable by cloning a nearby template variable and applying
    /// the desired name and type.
    /// </summary>
    /// <param name="existingVariables">Existing variables used as templates.</param>
    /// <param name="referenceIndex">Preferred template index.</param>
    /// <param name="desiredSpec">Desired variable specification.</param>
    /// <param name="plcOpenNs">PLCopen XML namespace.</param>
    /// <returns>New variable element with updated name and type.</returns>
    private static XElement CreateVariableFromTemplate(
        IReadOnlyList<XElement> existingVariables,
        int referenceIndex,
        GvlVariableSpec desiredSpec,
        XNamespace plcOpenNs)
    {
        XElement template = existingVariables.Count > 0
            ? existingVariables[Math.Clamp(referenceIndex, 0, existingVariables.Count - 1)]
            : BuildDefaultVariable(plcOpenNs);

        XElement clone = CloneVariableWithNameAndType(template, desiredSpec.VariableName, desiredSpec.CSharpType, plcOpenNs);
        clone.Elements(plcOpenNs + "documentation").Remove();
        return clone;
    }

    /// <summary>
    /// Clones a variable element and applies a new name and optional type override.
    /// </summary>
    /// <param name="sourceVariable">Source variable element.</param>
    /// <param name="newName">Target variable name.</param>
    /// <param name="csharpType">Optional C# type used for type mapping.</param>
    /// <param name="plcOpenNs">PLCopen XML namespace.</param>
    /// <returns>Cloned variable element.</returns>
    private static XElement CloneVariableWithNameAndType(
        XElement sourceVariable,
        string newName,
        string? csharpType,
        XNamespace plcOpenNs)
    {
        XElement clone = new(sourceVariable);
        clone.SetAttributeValue("name", newName);
        ApplyTypeToVariable(clone, csharpType, plcOpenNs);
        return clone;
    }

    /// <summary>
    /// Applies a mapped PLC type to a variable element.
    /// </summary>
    /// <param name="variable">Variable XML element to modify.</param>
    /// <param name="csharpType">Optional C# type to map.</param>
    /// <param name="plcOpenNs">PLCopen XML namespace.</param>
    private static void ApplyTypeToVariable(XElement variable, string? csharpType, XNamespace plcOpenNs)
    {
        if (string.IsNullOrWhiteSpace(csharpType))
            return;

        XElement? typeElement = variable.Element(plcOpenNs + "type");
        if (typeElement == null)
        {
            typeElement = new XElement(plcOpenNs + "type");
            variable.AddFirst(typeElement);
        }

        typeElement.RemoveNodes();
        typeElement.Add(new XElement(plcOpenNs + MapCSharpToPlcTypeElementName(csharpType)));
    }

    /// <summary>
    /// Builds a minimal default variable when no template variable exists.
    /// </summary>
    /// <param name="plcOpenNs">PLCopen XML namespace.</param>
    /// <returns>Default variable element.</returns>
    private static XElement BuildDefaultVariable(XNamespace plcOpenNs)
    {
        return new XElement(
            plcOpenNs + "variable",
            new XAttribute("name", "Placeholder"),
            new XElement(
                plcOpenNs + "type",
                new XElement(plcOpenNs + "string")));
    }

    /// <summary>
    /// Updates XML plain-text interface blocks by replacing old variable identifiers
    /// with new names using whole-word matching.
    /// </summary>
    /// <param name="doc">Loaded XML document.</param>
    /// <param name="renameMap">Mapping from old to new variable names.</param>
    private static void ApplyPlainTextRenames(XDocument doc, IReadOnlyDictionary<string, string> renameMap)
    {
        var replacements = renameMap
            .Where(pair => !string.Equals(pair.Key, pair.Value, StringComparison.Ordinal))
            .OrderByDescending(pair => pair.Key.Length)
            .ToList();

        if (replacements.Count == 0)
            return;

        var xhtmlNodes = doc
            .Descendants()
            .Where(node => string.Equals(node.Name.LocalName, "xhtml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var node in xhtmlNodes)
        {
            string sourceText = node.Value;
            if (sourceText.Length == 0)
                continue;

            string updatedText = sourceText;
            foreach (var replacement in replacements)
            {
                updatedText = Regex.Replace(
                    updatedText,
                    $@"\b{Regex.Escape(replacement.Key)}\b",
                    replacement.Value);
            }

            if (!string.Equals(sourceText, updatedText, StringComparison.Ordinal))
                node.Value = updatedText;
        }
    }

    /// <summary>
    /// Inserts declarations for newly added variables into plain-text interface blocks.
    /// </summary>
    /// <param name="doc">Loaded XML document.</param>
    /// <param name="insertedSpecs">List of inserted variable specifications.</param>
    private static void ApplyPlainTextInsertions(XDocument doc, IReadOnlyCollection<GvlVariableSpec> insertedSpecs)
    {
        var uniqueInsertions = insertedSpecs
            .GroupBy(spec => spec.VariableName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (uniqueInsertions.Count == 0)
            return;

        var xhtmlNodes = doc
            .Descendants()
            .Where(node => string.Equals(node.Name.LocalName, "xhtml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var node in xhtmlNodes)
        {
            string sourceText = node.Value;
            if (!sourceText.Contains("VAR_GLOBAL", StringComparison.Ordinal)
                || !sourceText.Contains("END_VAR", StringComparison.Ordinal))
            {
                continue;
            }

            int firstEndVarIndex = sourceText.IndexOf("END_VAR", StringComparison.Ordinal);
            if (firstEndVarIndex < 0)
                continue;

            var insertionBuilder = new StringBuilder();

            foreach (var insertion in uniqueInsertions)
            {
                if (Regex.IsMatch(sourceText, $@"\b{Regex.Escape(insertion.VariableName)}\b"))
                    continue;

                string plcTypeKeyword = MapCSharpToPlcTypeKeyword(insertion.CSharpType);
                insertionBuilder.AppendLine();
                insertionBuilder.AppendLine("\t{attribute 'OPC.UA.DA' := '1'}");
                insertionBuilder.AppendLine($"\t{insertion.VariableName} : {plcTypeKeyword};");
            }

            if (insertionBuilder.Length == 0)
                continue;

            string updatedText = sourceText.Insert(firstEndVarIndex, insertionBuilder.ToString());
            node.Value = updatedText;
        }
    }

    /// <summary>
    /// Normalizes a raw C# type string by removing namespace and nullable suffix.
    /// </summary>
    /// <param name="rawType">Raw type text.</param>
    /// <returns>Normalized simple type name or null.</returns>
    private static string? NormalizeCSharpTypeName(string? rawType)
    {
        if (string.IsNullOrWhiteSpace(rawType))
            return null;

        string typeName = rawType.Trim();
        int namespaceSeparator = typeName.LastIndexOf('.');
        if (namespaceSeparator >= 0)
            typeName = typeName[(namespaceSeparator + 1)..];

        if (typeName.EndsWith("?", StringComparison.Ordinal))
            typeName = typeName[..^1];

        return typeName;
    }

    /// <summary>
    /// Maps C# type names to PLCopen XML type element names.
    /// </summary>
    /// <param name="csharpType">C# type name.</param>
    /// <returns>PLCopen XML type element local name.</returns>
    private static string MapCSharpToPlcTypeElementName(string csharpType)
    {
        string normalized = NormalizeCSharpTypeName(csharpType) ?? "string";

        return normalized switch
        {
            "bool" or "Boolean" => "BOOL",
            "byte" or "Byte" => "BYTE",
            "sbyte" or "SByte" => "SINT",
            "ushort" or "UInt16" => "UINT",
            "short" or "Int16" => "INT",
            "uint" or "UInt32" => "UDINT",
            "int" or "Int32" => "DINT",
            "ulong" or "UInt64" => "ULINT",
            "long" or "Int64" => "LINT",
            "float" or "Single" => "REAL",
            "double" or "Double" => "LREAL",
            "DateTime" => "DT",
            "DateOnly" => "DATE",
            "TimeSpan" => "TIME",
            _ => "string"
        };
    }

    /// <summary>
    /// Maps C# type names to PLC textual type keywords used in interface plain text.
    /// </summary>
    /// <param name="csharpType">C# type name.</param>
    /// <returns>PLC type keyword.</returns>
    private static string MapCSharpToPlcTypeKeyword(string? csharpType)
    {
        string normalized = NormalizeCSharpTypeName(csharpType) ?? "string";

        return normalized switch
        {
            "bool" or "Boolean" => "BOOL",
            "byte" or "Byte" => "BYTE",
            "sbyte" or "SByte" => "SINT",
            "ushort" or "UInt16" => "UINT",
            "short" or "Int16" => "INT",
            "uint" or "UInt32" => "UDINT",
            "int" or "Int32" => "DINT",
            "ulong" or "UInt64" => "ULINT",
            "long" or "Int64" => "LINT",
            "float" or "Single" => "REAL",
            "double" or "Double" => "LREAL",
            "DateTime" => "DT",
            "DateOnly" => "DATE",
            "TimeSpan" => "TIME",
            _ => "STRING"
        };
    }

    /// <summary>
    /// Parses a text value into a boolean where "true" and "1" map to true.
    /// </summary>
    /// <param name="value">Text value to parse.</param>
    /// <returns>True when the value is truthy; otherwise false.</returns>
    private static bool ParseBool(string? value)
        => value != null && (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1"));

    private readonly record struct GvlVariableSpec(string GvlName, string VariableName, string? CSharpType);

    private sealed class NodeTupleComparer : IEqualityComparer<(string Gvl, string Name)>
    {
        public static readonly NodeTupleComparer OrdinalIgnoreCase = new(StringComparer.OrdinalIgnoreCase);

        private readonly StringComparer _comparer;

        /// <summary>
        /// Initializes the tuple comparer with a string comparer.
        /// </summary>
        /// <param name="comparer">Comparer used for tuple values.</param>
        private NodeTupleComparer(StringComparer comparer)
        {
            _comparer = comparer;
        }

        /// <summary>
        /// Compares two tuples by GVL and variable name.
        /// </summary>
        /// <param name="x">First tuple.</param>
        /// <param name="y">Second tuple.</param>
        /// <returns>True when both tuples are equal.</returns>
        public bool Equals((string Gvl, string Name) x, (string Gvl, string Name) y)
            => _comparer.Equals(x.Gvl, y.Gvl) && _comparer.Equals(x.Name, y.Name);

        /// <summary>
        /// Computes a hash code for tuple-based set and dictionary operations.
        /// </summary>
        /// <param name="obj">Tuple to hash.</param>
        /// <returns>Combined hash code.</returns>
        public int GetHashCode((string Gvl, string Name) obj)
            => HashCode.Combine(_comparer.GetHashCode(obj.Gvl), _comparer.GetHashCode(obj.Name));
    }
}
