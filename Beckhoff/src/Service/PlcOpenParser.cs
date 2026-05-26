using System.Xml.Linq;

namespace AmlParser.Modular.Service;

internal abstract record ParsedType;

internal sealed record PrimitiveType(string PlcName, string CsName) : ParsedType;

internal sealed record StringType(int? Length) : ParsedType;

internal sealed record DerivedType(string Name) : ParsedType;

internal sealed record ArrayType(string LowerBound, string UpperBound, ParsedType ElementType) : ParsedType;

internal sealed record PointerType(ParsedType BaseType) : ParsedType;

internal sealed record UnknownType(string RawName) : ParsedType;

internal sealed record ParsedVariable(
    string Name,
    ParsedType Type,
    bool IsConstant,
    string? InitialValueLiteral,
    string? Documentation);

internal sealed record ParsedGvl(
    string Name,
    IReadOnlyList<ParsedVariable> Variables);

internal sealed record ParsedDataType(
    string Name,
    string? ExtendsName,
    IReadOnlyList<ParsedVariable> Members);

internal sealed record ParsedModel(
    IReadOnlyList<ParsedGvl> Gvls,
    IReadOnlyList<ParsedDataType> DataTypes);

internal static class PlcOpenParser
{
    private static readonly XNamespace PlcOpenNs = "http://www.plcopen.org/xml/tc6_0200";

    /// <summary>
    /// Parses a PLCopen XML document into a structured model with all global
    /// variables and derived data types preserved.
    /// </summary>
    public static ParsedModel Parse(XDocument doc)
    {
        var gvls = ParseGlobalVariableBlocks(doc);
        var dataTypes = ParseDataTypes(doc);
        return new ParsedModel(gvls, dataTypes);
    }

    private static List<ParsedGvl> ParseGlobalVariableBlocks(XDocument doc)
    {
        var topLevelGvls = doc
            .Descendants(PlcOpenNs + "globalVars")
            .Where(globalVars => !globalVars.Ancestors().Any(a =>
                string.Equals(a.Name.LocalName, "MixedAttrsVarList", StringComparison.Ordinal)))
            .ToList();

        var byName = new Dictionary<string, List<ParsedVariable>>(StringComparer.Ordinal);
        var nameOrder = new List<string>();

        foreach (var globalVars in topLevelGvls)
        {
            string gvlName = (string?)globalVars.Attribute("name") ?? "GlobalVariables";
            bool blockIsConstant = ParseBool((string?)globalVars.Attribute("constant"));

            var constNames = CollectConstantNamesFromMixedAttrs(globalVars);

            foreach (var variable in globalVars.Elements(PlcOpenNs + "variable"))
            {
                string? name = (string?)variable.Attribute("name");
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                bool isConst = blockIsConstant || constNames.Contains(name);

                ParsedVariable parsed = new(
                    Name: name,
                    Type: ParseTypeElement(variable.Element(PlcOpenNs + "type")),
                    IsConstant: isConst,
                    InitialValueLiteral: ExtractInitialValueLiteral(variable.Element(PlcOpenNs + "initialValue")),
                    Documentation: ExtractDocumentationText(variable.Element(PlcOpenNs + "documentation")));

                if (!byName.TryGetValue(gvlName, out var list))
                {
                    list = new List<ParsedVariable>();
                    byName[gvlName] = list;
                    nameOrder.Add(gvlName);
                }

                if (!list.Any(v => string.Equals(v.Name, parsed.Name, StringComparison.Ordinal)))
                    list.Add(parsed);
            }
        }

        return nameOrder
            .Select(name => new ParsedGvl(name, byName[name]))
            .ToList();
    }

    private static HashSet<string> CollectConstantNamesFromMixedAttrs(XElement globalVars)
    {
        var constants = new HashSet<string>(StringComparer.Ordinal);

        var mixedLists = globalVars
            .Descendants(PlcOpenNs + "MixedAttrsVarList")
            .ToList();

        foreach (var mixedList in mixedLists)
        {
            foreach (var inner in mixedList.Elements(PlcOpenNs + "globalVars"))
            {
                if (!ParseBool((string?)inner.Attribute("constant")))
                    continue;

                foreach (var variable in inner.Elements(PlcOpenNs + "variable"))
                {
                    string? name = (string?)variable.Attribute("name");
                    if (!string.IsNullOrWhiteSpace(name))
                        constants.Add(name);
                }
            }
        }

        return constants;
    }

    private static List<ParsedDataType> ParseDataTypes(XDocument doc)
    {
        var result = new List<ParsedDataType>();

        foreach (var dataNode in doc.Descendants(PlcOpenNs + "data"))
        {
            string? dataName = (string?)dataNode.Attribute("name");
            if (!string.Equals(dataName, "http://www.3s-software.com/plcopenxml/datatype", StringComparison.Ordinal))
                continue;

            var dataType = dataNode.Element(PlcOpenNs + "dataType");
            if (dataType == null)
                continue;

            string? name = (string?)dataType.Attribute("name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var members = ParseDataTypeMembers(dataType);
            string? extendsName = ExtractDataTypeExtendsName(dataType);

            result.Add(new ParsedDataType(name, extendsName, members));
        }

        return result;
    }

    private static List<ParsedVariable> ParseDataTypeMembers(XElement dataType)
    {
        var members = new List<ParsedVariable>();

        var baseType = dataType.Element(PlcOpenNs + "baseType");
        var structElement = baseType?.Element(PlcOpenNs + "struct");
        if (structElement == null)
            return members;

        foreach (var variable in structElement.Elements(PlcOpenNs + "variable"))
        {
            string? name = (string?)variable.Attribute("name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            members.Add(new ParsedVariable(
                Name: name,
                Type: ParseTypeElement(variable.Element(PlcOpenNs + "type")),
                IsConstant: false,
                InitialValueLiteral: ExtractInitialValueLiteral(variable.Element(PlcOpenNs + "initialValue")),
                Documentation: ExtractDocumentationText(variable.Element(PlcOpenNs + "documentation"))));
        }

        return members;
    }

    private static string? ExtractDataTypeExtendsName(XElement dataType)
    {
        foreach (var data in dataType.Descendants(PlcOpenNs + "data"))
        {
            string? dataName = (string?)data.Attribute("name");
            if (!string.Equals(dataName, "http://www.3s-software.com/plcopenxml/datatypeinheritance", StringComparison.Ordinal))
                continue;

            var extendsElement = data.Descendants(PlcOpenNs + "Extends").FirstOrDefault();
            string? value = extendsElement?.Value.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static ParsedType ParseTypeElement(XElement? typeElement)
    {
        if (typeElement == null)
            return new UnknownType("unknown");

        var inner = typeElement.Elements().FirstOrDefault();
        if (inner == null)
            return new UnknownType("empty");

        string localName = inner.Name.LocalName;
        string upper = localName.ToUpperInvariant();

        switch (upper)
        {
            case "STRING":
            case "WSTRING":
                {
                    int? length = TryParseInt((string?)inner.Attribute("length"));
                    return new StringType(length);
                }
            case "ARRAY":
                return ParseArrayType(inner);
            case "POINTER":
                {
                    var baseTypeInner = inner.Element(PlcOpenNs + "baseType");
                    return new PointerType(ParseTypeElement(baseTypeInner != null
                        ? new XElement(typeElement.Name, baseTypeInner.Elements())
                        : null));
                }
            case "DERIVED":
                {
                    string? name = (string?)inner.Attribute("name");
                    return string.IsNullOrWhiteSpace(name)
                        ? new UnknownType("derived")
                        : new DerivedType(name);
                }
        }

        return MapPrimitive(localName);
    }

    private static ParsedType ParseArrayType(XElement arrayElement)
    {
        var dimension = arrayElement.Element(PlcOpenNs + "dimension");
        string lower = (string?)dimension?.Attribute("lower") ?? "0";
        string upper = (string?)dimension?.Attribute("upper") ?? "0";

        var baseTypeElement = arrayElement.Element(PlcOpenNs + "baseType");
        ParsedType elementType = new UnknownType("array-element");

        if (baseTypeElement != null)
        {
            var inner = baseTypeElement.Elements().FirstOrDefault();
            if (inner != null)
            {
                var wrapper = new XElement(PlcOpenNs + "type", inner);
                elementType = ParseTypeElement(wrapper);
            }
        }

        return new ArrayType(lower, upper, elementType);
    }

    private static ParsedType MapPrimitive(string plcType)
    {
        string cs = plcType.ToUpperInvariant() switch
        {
            "BOOL" => "bool",
            "BYTE" => "byte",
            "USINT" => "byte",
            "SINT" => "sbyte",
            "UINT" => "ushort",
            "INT" => "short",
            "WORD" => "ushort",
            "UDINT" => "uint",
            "DWORD" => "uint",
            "DINT" => "int",
            "ULINT" => "ulong",
            "LWORD" => "ulong",
            "LINT" => "long",
            "REAL" => "float",
            "LREAL" => "double",
            "DT" => "System.DateTime",
            "DATE" => "System.DateOnly",
            "TIME" => "System.TimeSpan",
            "BIT" => "bool",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(cs))
            return new UnknownType(plcType);

        return new PrimitiveType(plcType, cs);
    }

    private static string? ExtractInitialValueLiteral(XElement? initialValue)
    {
        if (initialValue == null)
            return null;

        var simple = initialValue.Element(PlcOpenNs + "simpleValue");
        string? value = (string?)simple?.Attribute("value");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? ExtractDocumentationText(XElement? documentation)
    {
        if (documentation == null)
            return null;

        string text = string.Concat(documentation.DescendantNodes()
            .OfType<XText>()
            .Select(t => t.Value));

        text = text.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static int? TryParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return int.TryParse(value, out int parsed) ? parsed : null;
    }

    private static bool ParseBool(string? value)
        => value != null && (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1"));
}
