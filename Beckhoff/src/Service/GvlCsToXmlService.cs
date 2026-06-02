using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace XmlParser.Modular.Service;

public sealed class GvlCsToXmlService : IGvlCsToXmlService
{
    private static readonly XNamespace PlcOpenNs = "http://www.plcopen.org/xml/tc6_0200";
    private static readonly XNamespace XhtmlNs = "http://www.w3.org/1999/xhtml";

    private static readonly Regex ClassRegex = new(
        @"public\s+(?:sealed\s+|abstract\s+|static\s+)?class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?::\s*(?<base>[A-Za-z_][A-Za-z0-9_]*))?\s*\{(?<body>(?:[^{}]|\{(?:[^{}]|\{[^{}]*\})*\})*)\}",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex PropertyRegex = new(
        @"(?<doc>(?:\s*///[^\n]*\n)*)?\s*public\s+(?:(?:virtual|override|sealed|new)\s+)*(?<type>[A-Za-z_][A-Za-z0-9_\.\[\]]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get\s*;\s*set\s*;\s*\}(?:\s*=\s*(?<init>[^;]+)\s*;)?",
        RegexOptions.Compiled);

    private static readonly Regex ConstRegex = new(
        @"(?<doc>(?:\s*///[^\n]*\n)*)?\s*public\s+const\s+(?<type>[A-Za-z_][A-Za-z0-9_\.\[\]]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<init>[^;]+);",
        RegexOptions.Compiled);

    private static readonly Regex StaticReadonlyRegex = new(
        @"(?<doc>(?:\s*///[^\n]*\n)*)?\s*public\s+static\s+readonly\s+(?<type>[A-Za-z_][A-Za-z0-9_\.\[\]]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:=\s*(?<init>[^;]+))?\s*;",
        RegexOptions.Compiled);

    private static readonly Regex ProxyNodeRegex = new(
        @"ReadValueFromPlcNode\s*<\s*(?<type>[^>]+)\s*>\s*\(\s*""(?<gvl>[A-Za-z_][A-Za-z0-9_]*)\.(?<name>[A-Za-z_][A-Za-z0-9_]*)""",
        RegexOptions.Compiled);

    private static readonly Regex StubMarkerRegex = new(
        @"//\s*Stub placeholders", RegexOptions.Compiled);

    public void GenerateXmlFromCSharp(string gvlCsPath, string? gvlProxyCsPath, string outputXmlPath)
    {
        if (!File.Exists(gvlCsPath))
            throw new FileNotFoundException("Gvl.cs not found", gvlCsPath);

        string gvlSource = File.ReadAllText(gvlCsPath);
        string? proxySource = !string.IsNullOrWhiteSpace(gvlProxyCsPath) && File.Exists(gvlProxyCsPath)
            ? File.ReadAllText(gvlProxyCsPath)
            : null;

        var stubNames = ParseStubClassNames(gvlSource);
        var allClasses = ParseClasses(gvlSource);

        var classes = allClasses
            .Where(c => !stubNames.Contains(c.Name))
            .Where(c => c.Members.Count > 0)
            .ToList();

        var proxyGvlNames = proxySource != null
            ? ExtractProxyGvlNames(proxySource)
            : new HashSet<string>(StringComparer.Ordinal);

        ClassifyClasses(classes, proxyGvlNames, out var gvlClasses, out var dataTypeClasses);

        var fileName = Path.GetFileNameWithoutExtension(outputXmlPath);
        var doc = BuildXmlDocument(fileName, gvlClasses, dataTypeClasses);

        Directory.CreateDirectory(Path.GetDirectoryName(outputXmlPath) ?? ".");
        var writerSettings = new System.Xml.XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(false),
        };
        using var writer = System.Xml.XmlWriter.Create(outputXmlPath, writerSettings);
        doc.Save(writer);
    }

    private static HashSet<string> ParseStubClassNames(string source)
    {
        var stubs = new HashSet<string>(StringComparer.Ordinal);
        var markerMatch = StubMarkerRegex.Match(source);
        if (!markerMatch.Success)
            return stubs;

        int sectionStart = markerMatch.Index;
        int sectionEnd = source.Length;

        var emptyClassRegex = new Regex(
            @"public\s+class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*\}",
            RegexOptions.Compiled);

        foreach (Match match in emptyClassRegex.Matches(source))
        {
            if (match.Index >= sectionStart && match.Index < sectionEnd)
                stubs.Add(match.Groups["name"].Value);
        }

        return stubs;
    }

    private static List<CsClass> ParseClasses(string source)
    {
        var result = new List<CsClass>();

        foreach (Match match in ClassRegex.Matches(source))
        {
            string name = match.Groups["name"].Value;
            string? baseName = match.Groups["base"].Success ? match.Groups["base"].Value : null;
            string body = match.Groups["body"].Value;

            var members = new List<CsMember>();
            members.AddRange(ParseMembers(body, MemberKind.Property));
            members.AddRange(ParseMembers(body, MemberKind.Const));
            members.AddRange(ParseMembers(body, MemberKind.StaticReadonly));

            var ordered = OrderMembersBySourceOrder(body, members);
            result.Add(new CsClass(name, baseName, ordered));
        }

        return result;
    }

    private static IEnumerable<CsMember> ParseMembers(string body, MemberKind kind)
    {
        var regex = kind switch
        {
            MemberKind.Property => PropertyRegex,
            MemberKind.Const => ConstRegex,
            MemberKind.StaticReadonly => StaticReadonlyRegex,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        foreach (Match match in regex.Matches(body))
        {
            if (kind == MemberKind.Property)
            {
                if (LooksLikeStaticReadonly(match.Value) || LooksLikeConst(match.Value))
                    continue;
            }

            if (kind == MemberKind.StaticReadonly && LooksLikeConst(match.Value))
                continue;

            string memberName = match.Groups["name"].Value;
            string memberType = match.Groups["type"].Value.Trim();
            string? initializer = match.Groups["init"].Success
                ? match.Groups["init"].Value.Trim()
                : null;
            string? docSummary = ParseDocSummary(match.Groups["doc"].Value);

            yield return new CsMember(
                Name: memberName,
                CsType: memberType,
                Kind: kind,
                Initializer: initializer,
                DocSummary: docSummary,
                SourceIndex: match.Index);
        }
    }

    private static bool LooksLikeConst(string snippet) =>
        Regex.IsMatch(snippet, @"\bpublic\s+const\b");

    private static bool LooksLikeStaticReadonly(string snippet) =>
        Regex.IsMatch(snippet, @"\bpublic\s+static\s+readonly\b");

    private static List<CsMember> OrderMembersBySourceOrder(string _, IEnumerable<CsMember> members)
    {
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        var ordered = members
            .OrderBy(m => m.SourceIndex)
            .Where(m => seenNames.Add(m.Name))
            .ToList();
        return ordered;
    }

    private static string? ParseDocSummary(string? rawDoc)
    {
        if (string.IsNullOrWhiteSpace(rawDoc))
            return null;

        var lines = rawDoc
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("///"))
            .Select(line => line[3..].Trim())
            .Where(line =>
                !line.StartsWith("<summary", StringComparison.OrdinalIgnoreCase) &&
                !line.StartsWith("</summary", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (lines.Count == 0)
            return null;

        return string.Join("\n", lines).Trim();
    }

    private static HashSet<string> ExtractProxyGvlNames(string proxySource)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in ProxyNodeRegex.Matches(proxySource))
            names.Add(match.Groups["gvl"].Value);
        return names;
    }

    private static void ClassifyClasses(
        IReadOnlyList<CsClass> classes,
        HashSet<string> proxyGvlNames,
        out List<CsClass> gvlClasses,
        out List<CsClass> dataTypeClasses)
    {
        var classNameSet = new HashSet<string>(classes.Select(c => c.Name), StringComparer.Ordinal);

        var referencedAsType = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cls in classes)
        {
            foreach (var member in cls.Members)
            {
                string baseTypeName = StripArraySuffix(member.CsType);
                if (classNameSet.Contains(baseTypeName))
                    referencedAsType.Add(baseTypeName);
            }
        }

        gvlClasses = new List<CsClass>();
        dataTypeClasses = new List<CsClass>();

        foreach (var cls in classes)
        {
            bool isProxyGvl = proxyGvlNames.Contains(cls.Name);
            bool isReferenced = referencedAsType.Contains(cls.Name);
            bool hasGvlPrefix =
                cls.Name.StartsWith("GVL", StringComparison.OrdinalIgnoreCase) ||
                cls.Name.StartsWith("Global", StringComparison.OrdinalIgnoreCase);

            if (isReferenced)
                dataTypeClasses.Add(cls);
            else if (isProxyGvl || hasGvlPrefix)
                gvlClasses.Add(cls);
            else
                dataTypeClasses.Add(cls);
        }
    }

    private static string StripArraySuffix(string type)
    {
        int bracket = type.IndexOf('[');
        return bracket >= 0 ? type[..bracket] : type;
    }

    private static XDocument BuildXmlDocument(
        string projectName,
        IReadOnlyList<CsClass> gvlClasses,
        IReadOnlyList<CsClass> dataTypeClasses)
    {
        string nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);

        var project = new XElement(
            PlcOpenNs + "project",
            new XElement(
                PlcOpenNs + "fileHeader",
                new XAttribute("companyName", "Beckhoff Automation GmbH"),
                new XAttribute("productName", "TwinCAT PLC Control"),
                new XAttribute("productVersion", "3.5.13.21"),
                new XAttribute("creationDateTime", nowIso)),
            new XElement(
                PlcOpenNs + "contentHeader",
                new XAttribute("name", projectName),
                new XAttribute("modificationDateTime", nowIso),
                new XElement(
                    PlcOpenNs + "coordinateInfo",
                    BuildScaling("fbd"),
                    BuildScaling("ld"),
                    BuildScaling("sfc")),
                new XElement(
                    PlcOpenNs + "addData",
                    new XElement(
                        PlcOpenNs + "data",
                        new XAttribute("name", "http://www.3s-software.com/plcopenxml/projectinformation"),
                        new XAttribute("handleUnknown", "implementation"),
                        new XElement(PlcOpenNs + "ProjectInformation")))),
            new XElement(
                PlcOpenNs + "types",
                new XElement(PlcOpenNs + "dataTypes"),
                new XElement(PlcOpenNs + "pous")),
            new XElement(
                PlcOpenNs + "instances",
                new XElement(PlcOpenNs + "configurations")),
            BuildApplicationAddData(projectName, gvlClasses, dataTypeClasses));

        return new XDocument(new XDeclaration("1.0", "utf-8", null), project);
    }

    private static XElement BuildScaling(string mode) =>
        new(PlcOpenNs + mode,
            new XElement(PlcOpenNs + "scaling",
                new XAttribute("x", "1"),
                new XAttribute("y", "1")));

    private static XElement BuildApplicationAddData(
        string projectName,
        IReadOnlyList<CsClass> gvlClasses,
        IReadOnlyList<CsClass> dataTypeClasses)
    {
        var resource = new XElement(
            PlcOpenNs + "resource",
            new XAttribute("name", projectName));

        AddDefaultTasks(resource);

        foreach (var gvl in gvlClasses)
            resource.Add(BuildGlobalVarsElement(gvl));

        foreach (var dt in dataTypeClasses)
            resource.Add(BuildDataTypeAddDataElement(dt));

        return new XElement(
            PlcOpenNs + "addData",
            new XElement(
                PlcOpenNs + "data",
                new XAttribute("name", "http://www.3s-software.com/plcopenxml/application"),
                new XAttribute("handleUnknown", "implementation"),
                resource));
    }

    private static void AddDefaultTasks(XElement resource)
    {
        resource.Add(BuildTask("PlcTask", "MAIN", 20, intervalMicros: 10000));
        resource.Add(BuildTask("PlcTask1ms", "Background", 4, intervalMicros: 1000));
    }

    private static XElement BuildTask(string taskName, string pouName, int priority, int intervalMicros)
    {
        return new XElement(
            PlcOpenNs + "task",
            new XAttribute("name", taskName),
            new XAttribute("interval", "PT0S"),
            new XAttribute("priority", priority.ToString(CultureInfo.InvariantCulture)),
            new XElement(
                PlcOpenNs + "pouInstance",
                new XAttribute("name", pouName),
                new XAttribute("typeName", ""),
                new XElement(
                    PlcOpenNs + "documentation",
                    new XElement(XhtmlNs + "xhtml"))),
            new XElement(
                PlcOpenNs + "addData",
                new XElement(
                    PlcOpenNs + "data",
                    new XAttribute("name", "http://www.3s-software.com/plcopenxml/tasksettings"),
                    new XAttribute("handleUnknown", "implementation"),
                    new XElement(
                        PlcOpenNs + "TaskSettings",
                        new XAttribute("KindOfTask", "Cyclic"),
                        new XAttribute("Interval", intervalMicros.ToString(CultureInfo.InvariantCulture)),
                        new XAttribute("IntervalUnit", "us"),
                        new XElement(
                            PlcOpenNs + "Watchdog",
                            new XAttribute("Enabled", "false"),
                            new XAttribute("TimeUnit", "ms")))),
                BuildObjectIdData()));
    }

    private static XElement BuildObjectIdData()
    {
        return new XElement(
            PlcOpenNs + "data",
            new XAttribute("name", "http://www.3s-software.com/plcopenxml/objectid"),
            new XAttribute("handleUnknown", "discard"),
            new XElement(PlcOpenNs + "ObjectId", Guid.NewGuid().ToString("D")));
    }

    private static XElement BuildGlobalVarsElement(CsClass gvl)
    {
        bool allConstant = gvl.Members.All(IsConstantLike);
        var globalVars = new XElement(PlcOpenNs + "globalVars", new XAttribute("name", gvl.Name));
        if (allConstant && gvl.Members.Count > 0)
            globalVars.Add(new XAttribute("constant", "true"));

        foreach (var member in gvl.Members)
            globalVars.Add(BuildVariableElement(member, includeOpcAttribute: !IsConstantLike(member)));

        var addData = new XElement(PlcOpenNs + "addData");
        addData.Add(BuildAttributeData("qualified_only", ""));
        addData.Add(BuildBuildPropertiesData());
        addData.Add(BuildInterfaceAsPlainTextData(gvl));
        addData.Add(BuildObjectIdData());

        bool hasMixedConsts = HasMixedConsts(gvl);
        if (hasMixedConsts)
            addData.Add(BuildMixedAttrsVarListData(gvl));

        globalVars.Add(addData);
        return globalVars;
    }

    private static bool HasMixedConsts(CsClass gvl)
    {
        bool anyConst = gvl.Members.Any(IsConstantLike);
        bool anyNonConst = gvl.Members.Any(m => !IsConstantLike(m));
        return anyConst && anyNonConst;
    }

    private static bool IsConstantLike(CsMember member) =>
        member.Kind == MemberKind.Const || member.Kind == MemberKind.StaticReadonly;

    private static XElement BuildVariableElement(CsMember member, bool includeOpcAttribute)
    {
        var variable = new XElement(PlcOpenNs + "variable", new XAttribute("name", member.Name));
        variable.Add(BuildTypeElement(member.CsType, ExtractArrayUpperBound(member)));

        string? initialLiteral = ConvertInitializerToPlcLiteral(member);
        if (initialLiteral != null)
        {
            variable.Add(
                new XElement(
                    PlcOpenNs + "initialValue",
                    new XElement(
                        PlcOpenNs + "simpleValue",
                        new XAttribute("value", initialLiteral))));
        }

        if (includeOpcAttribute)
        {
            variable.Add(
                new XElement(
                    PlcOpenNs + "addData",
                    BuildAttributeData("OPC.UA.DA", "1")));
        }

        if (!string.IsNullOrWhiteSpace(member.DocSummary))
        {
            variable.Add(
                new XElement(
                    PlcOpenNs + "documentation",
                    new XElement(XhtmlNs + "xhtml", member.DocSummary)));
        }

        return variable;
    }

    private static XElement BuildTypeElement(string csType, int arrayUpperBound)
    {
        string trimmed = csType.Trim();
        bool isArray = trimmed.EndsWith("[]", StringComparison.Ordinal);
        string baseType = isArray ? trimmed[..^2].Trim() : trimmed;

        if (isArray)
        {
            return new XElement(
                PlcOpenNs + "type",
                new XElement(
                    PlcOpenNs + "array",
                    new XElement(
                        PlcOpenNs + "dimension",
                        new XAttribute("lower", "1"),
                        new XAttribute("upper", arrayUpperBound.ToString(CultureInfo.InvariantCulture))),
                    new XElement(
                        PlcOpenNs + "baseType",
                        BuildInnerType(baseType))));
        }

        return new XElement(PlcOpenNs + "type", BuildInnerType(baseType));
    }

    private static readonly Regex ArraySizeRegex = new(
        @"new\s+[A-Za-z_][A-Za-z0-9_\.]*\s*\[\s*(?<size>\d+)\s*\]",
        RegexOptions.Compiled);

    private static int ExtractArrayUpperBound(CsMember member)
    {
        if (string.IsNullOrWhiteSpace(member.Initializer))
            return 1;

        var match = ArraySizeRegex.Match(member.Initializer);
        if (match.Success && int.TryParse(match.Groups["size"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int size) && size > 0)
            return size;

        return 1;
    }

    private static XElement BuildInnerType(string baseType)
    {
        string normalized = NormalizeCsTypeName(baseType);
        string? plcKeyword = MapCsToPlcKeyword(normalized);

        if (plcKeyword == "STRING")
            return new XElement(PlcOpenNs + "string");

        if (plcKeyword != null)
            return new XElement(PlcOpenNs + plcKeyword);

        return new XElement(PlcOpenNs + "derived", new XAttribute("name", normalized));
    }

    private static string NormalizeCsTypeName(string csType)
    {
        string trimmed = csType.Trim().TrimEnd('?');
        int dot = trimmed.LastIndexOf('.');
        return dot >= 0 ? trimmed[(dot + 1)..] : trimmed;
    }

    private static string? MapCsToPlcKeyword(string normalizedCsType)
    {
        return normalizedCsType switch
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
            "string" or "String" => "STRING",
            _ => null
        };
    }

    private static string? ConvertInitializerToPlcLiteral(CsMember member)
    {
        if (string.IsNullOrWhiteSpace(member.Initializer))
            return null;

        string raw = member.Initializer.Trim();

        if (raw.StartsWith("new ", StringComparison.Ordinal))
            return null;
        if (raw.StartsWith("System.Array", StringComparison.Ordinal))
            return null;
        if (raw == "string.Empty" || raw == "String.Empty")
            return null;

        if (raw.StartsWith("\"", StringComparison.Ordinal) && raw.EndsWith("\"", StringComparison.Ordinal) && raw.Length >= 2)
        {
            string inner = raw[1..^1].Replace("\\\"", "\"").Replace("\\\\", "\\");
            return inner;
        }

        if (raw == "true") return "TRUE";
        if (raw == "false") return "FALSE";

        if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            string hex = raw[2..].TrimEnd('u', 'U', 'l', 'L');
            return "16#" + hex;
        }

        if (raw.EndsWith("f", StringComparison.OrdinalIgnoreCase) || raw.EndsWith("d", StringComparison.OrdinalIgnoreCase))
            raw = raw[..^1];

        return raw;
    }

    private static XElement BuildAttributeData(string attributeName, string attributeValue)
    {
        return new XElement(
            PlcOpenNs + "data",
            new XAttribute("name", "http://www.3s-software.com/plcopenxml/attributes"),
            new XAttribute("handleUnknown", "implementation"),
            new XElement(
                PlcOpenNs + "Attributes",
                new XElement(
                    PlcOpenNs + "Attribute",
                    new XAttribute("Name", attributeName),
                    new XAttribute("Value", attributeValue))));
    }

    private static XElement BuildBuildPropertiesData()
    {
        return new XElement(
            PlcOpenNs + "data",
            new XAttribute("name", "http://www.3s-software.com/plcopenxml/buildproperties"),
            new XAttribute("handleUnknown", "implementation"),
            new XElement(
                PlcOpenNs + "BuildProperties",
                new XElement(PlcOpenNs + "LinkAlways", "true")));
    }

    private static XElement BuildInterfaceAsPlainTextData(CsClass gvl)
    {
        var nonConsts = gvl.Members.Where(m => !IsConstantLike(m)).ToList();
        var consts = gvl.Members.Where(IsConstantLike).ToList();

        var sb = new StringBuilder();
        sb.Append("{attribute 'qualified_only'}\n");

        if (nonConsts.Count > 0)
        {
            sb.Append("VAR_GLOBAL\n");
            foreach (var member in nonConsts)
            {
                sb.Append("\t{attribute 'OPC.UA.DA' := '1'}\n");
                sb.Append('\t').Append(RenderPlainTextDeclaration(member)).Append('\n');
            }
            sb.Append("END_VAR\n");
        }

        if (consts.Count > 0)
        {
            sb.Append("VAR_GLOBAL CONSTANT\n");
            foreach (var member in consts)
            {
                sb.Append('\t').Append(RenderPlainTextDeclaration(member)).Append('\n');
            }
            sb.Append("END_VAR\n");
        }

        return new XElement(
            PlcOpenNs + "data",
            new XAttribute("name", "http://www.3s-software.com/plcopenxml/interfaceasplaintext"),
            new XAttribute("handleUnknown", "implementation"),
            new XElement(
                PlcOpenNs + "InterfaceAsPlainText",
                new XElement(XhtmlNs + "xhtml", sb.ToString())));
    }

    private static string RenderPlainTextDeclaration(CsMember member)
    {
        string typeText = RenderPlainTextType(member.CsType, ExtractArrayUpperBound(member));
        string? initText = ConvertInitializerToPlcLiteral(member);
        string assignment = initText != null ? $" := {initText}" : string.Empty;
        return $"{member.Name} : {typeText}{assignment};";
    }

    private static string RenderPlainTextType(string csType, int arrayUpperBound)
    {
        string trimmed = csType.Trim();
        bool isArray = trimmed.EndsWith("[]", StringComparison.Ordinal);
        string baseType = isArray ? trimmed[..^2].Trim() : trimmed;
        string normalized = NormalizeCsTypeName(baseType);
        string plcType = MapCsToPlcKeyword(normalized) ?? normalized;
        return isArray ? $"ARRAY[1..{arrayUpperBound}] OF {plcType}" : plcType;
    }

    private static XElement BuildMixedAttrsVarListData(CsClass gvl)
    {
        var nonConsts = gvl.Members.Where(m => !IsConstantLike(m)).ToList();
        var consts = gvl.Members.Where(IsConstantLike).ToList();

        var mixed = new XElement(PlcOpenNs + "MixedAttrsVarList");

        if (nonConsts.Count > 0)
        {
            var nonConstBlock = new XElement(
                PlcOpenNs + "globalVars",
                new XAttribute("name", gvl.Name));
            foreach (var m in nonConsts)
                nonConstBlock.Add(BuildVariableElement(m, includeOpcAttribute: true));
            mixed.Add(nonConstBlock);
        }

        if (consts.Count > 0)
        {
            var constBlock = new XElement(
                PlcOpenNs + "globalVars",
                new XAttribute("name", gvl.Name),
                new XAttribute("constant", "true"));
            foreach (var m in consts)
                constBlock.Add(BuildVariableElement(m, includeOpcAttribute: false));
            mixed.Add(constBlock);
        }

        return new XElement(
            PlcOpenNs + "data",
            new XAttribute("name", "http://www.3s-software.com/plcopenxml/mixedattrsvarlist"),
            new XAttribute("handleUnknown", "implementation"),
            mixed);
    }

    private static XElement BuildDataTypeAddDataElement(CsClass dt)
    {
        var structElement = new XElement(PlcOpenNs + "struct");
        foreach (var member in dt.Members)
            structElement.Add(BuildVariableElement(member, includeOpcAttribute: !IsConstantLike(member)));

        var dataType = new XElement(
            PlcOpenNs + "dataType",
            new XAttribute("name", dt.Name),
            new XElement(PlcOpenNs + "baseType", structElement));

        var addData = new XElement(PlcOpenNs + "addData");

        if (!string.IsNullOrWhiteSpace(dt.BaseClass))
        {
            addData.Add(
                new XElement(
                    PlcOpenNs + "data",
                    new XAttribute("name", "http://www.3s-software.com/plcopenxml/datatypeinheritance"),
                    new XAttribute("handleUnknown", "implementation"),
                    new XElement(
                        PlcOpenNs + "Inheritance",
                        new XElement(PlcOpenNs + "Extends", dt.BaseClass))));
        }

        addData.Add(BuildDataTypePlainTextData(dt));
        addData.Add(BuildObjectIdData());

        dataType.Add(addData);

        return new XElement(
            PlcOpenNs + "data",
            new XAttribute("name", "http://www.3s-software.com/plcopenxml/datatype"),
            new XAttribute("handleUnknown", "implementation"),
            dataType);
    }

    private static XElement BuildDataTypePlainTextData(CsClass dt)
    {
        var sb = new StringBuilder();
        string extendsClause = !string.IsNullOrWhiteSpace(dt.BaseClass)
            ? $" EXTENDS {dt.BaseClass}"
            : string.Empty;

        sb.Append("TYPE ").Append(dt.Name).Append(extendsClause).Append(" :\n");
        sb.Append("STRUCT\n");
        foreach (var member in dt.Members)
        {
            if (!IsConstantLike(member))
                sb.Append("\t{attribute 'OPC.UA.DA' := '1'}\n");
            sb.Append('\t').Append(RenderPlainTextDeclaration(member)).Append('\n');
        }
        sb.Append("END_STRUCT\n");
        sb.Append("END_TYPE\n");

        return new XElement(
            PlcOpenNs + "data",
            new XAttribute("name", "http://www.3s-software.com/plcopenxml/interfaceasplaintext"),
            new XAttribute("handleUnknown", "implementation"),
            new XElement(
                PlcOpenNs + "InterfaceAsPlainText",
                new XElement(XhtmlNs + "xhtml", sb.ToString())));
    }

    private enum MemberKind { Property, Const, StaticReadonly }

    private sealed record CsMember(
        string Name,
        string CsType,
        MemberKind Kind,
        string? Initializer,
        string? DocSummary,
        int SourceIndex);

    private sealed record CsClass(
        string Name,
        string? BaseClass,
        IReadOnlyList<CsMember> Members);
}
