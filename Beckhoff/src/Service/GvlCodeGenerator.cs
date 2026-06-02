using System.Text;
using System.Text.RegularExpressions;

namespace XmlParser.Modular.Service;

internal sealed class GvlCodeGenerator
{
    private readonly ParsedModel _model;
    private readonly PlcStatusControlConfig _config;
    private readonly IReadOnlyDictionary<string, int> _constantLookup;
    private readonly HashSet<string> _definedDataTypeNames;
    private readonly HashSet<string> _enumTypeNames;

    public GvlCodeGenerator(ParsedModel model, PlcStatusControlConfig config)
    {
        _model = model;
        _config = config;
        _constantLookup = BuildConstantLookup(model);
        _definedDataTypeNames = new HashSet<string>(
            model.DataTypes.Select(d => d.Name),
            StringComparer.Ordinal);
        _enumTypeNames = new HashSet<string>(model.EnumTypeNames, StringComparer.Ordinal);
    }

    public string EmitGvlFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        EmitAdditionalUsings(sb, _config.AdditionalUsings);
        sb.AppendLine();

        bool hasNamespace = !string.IsNullOrWhiteSpace(_config.Namespace);
        if (hasNamespace)
        {
            sb.AppendLine($"namespace {_config.Namespace}");
            sb.AppendLine("{");
        }

        string indent = hasNamespace ? "    " : string.Empty;

        EmitStubsForUndefinedDerivedTypes(sb, indent);
        EmitDataTypes(sb, indent);
        EmitGvlClasses(sb, indent);

        if (hasNamespace)
            sb.AppendLine("}");

        return sb.ToString();
    }

    public string EmitProxyFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        if (!string.IsNullOrWhiteSpace(_config.HardwareUsing))
            sb.AppendLine($"using {_config.HardwareUsing};");
        EmitAdditionalUsings(sb, _config.AdditionalProxyUsings);
        sb.AppendLine();

        bool hasNamespace = !string.IsNullOrWhiteSpace(_config.Namespace);
        if (hasNamespace)
        {
            sb.AppendLine($"namespace {_config.Namespace}");
            sb.AppendLine("{");
        }

        string indent = hasNamespace ? "    " : string.Empty;

        // Emit one proxy class per GVL. Each proxy inherits its GVL and overrides
        // the variables so reads fetch live PLC values into a cached model.
        bool firstEmitted = false;
        foreach (var gvl in _model.Gvls)
        {
            var proxyVars = gvl.Variables.Where(v => !v.IsConstant).ToList();
            if (proxyVars.Count == 0)
                continue;

            if (firstEmitted)
                sb.AppendLine();
            firstEmitted = true;

            EmitProxyClass(sb, indent, gvl.Name, proxyVars);
        }

        if (hasNamespace)
            sb.AppendLine("}");

        return sb.ToString();
    }

    // Emits one "using X;" line per configured entry, tolerating values that
    // already include the 'using' keyword or a trailing semicolon.
    private static void EmitAdditionalUsings(StringBuilder sb, IReadOnlyList<string> usings)
    {
        foreach (var raw in usings)
        {
            string ns = raw.Trim();
            if (ns.StartsWith("using ", StringComparison.Ordinal))
                ns = ns["using ".Length..].Trim();
            ns = ns.TrimEnd(';').Trim();
            if (ns.Length == 0)
                continue;
            sb.AppendLine($"using {ns};");
        }
    }

    private void EmitStubsForUndefinedDerivedTypes(StringBuilder sb, string indent)
    {
        var referenced = new HashSet<string>(StringComparer.Ordinal);

        foreach (var gvl in _model.Gvls)
            foreach (var v in gvl.Variables)
                CollectDerivedNames(v.Type, referenced);

        foreach (var dt in _model.DataTypes)
            foreach (var m in dt.Members)
                CollectDerivedNames(m.Type, referenced);

        var undefined = referenced
            .Where(name => !_definedDataTypeNames.Contains(name))
            .Where(name => !_enumTypeNames.Contains(name)) // enums are external typed references, no stub
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (undefined.Count == 0)
            return;

        sb.AppendLine($"{indent}// Stub placeholders for derived types referenced but not defined in this XML.");
        sb.AppendLine($"{indent}// Fill in members manually or replace with project library imports.");
        foreach (var name in undefined)
        {
            sb.AppendLine($"{indent}public class {SanitizeIdentifier(name)}");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }
    }

    private void EmitDataTypes(StringBuilder sb, string indent)
    {
        var emitted = new HashSet<string>(StringComparer.Ordinal);
        var pending = new List<ParsedDataType>(_model.DataTypes);

        while (pending.Count > 0)
        {
            var ready = pending
                .Where(dt => string.IsNullOrEmpty(dt.ExtendsName)
                          || emitted.Contains(dt.ExtendsName!)
                          || !_definedDataTypeNames.Contains(dt.ExtendsName!))
                .ToList();

            if (ready.Count == 0)
            {
                // Cycle or unresolved dependency — emit remaining as-is.
                ready = new List<ParsedDataType>(pending);
            }

            foreach (var dt in ready)
            {
                EmitDataType(sb, indent, dt);
                emitted.Add(dt.Name);
                pending.Remove(dt);
            }
        }
    }

    private void EmitDataType(StringBuilder sb, string indent, ParsedDataType dt)
    {
        string className = SanitizeIdentifier(dt.Name);
        string? baseClause = !string.IsNullOrWhiteSpace(dt.ExtendsName)
            ? $" : {SanitizeIdentifier(dt.ExtendsName!)}"
            : null;

        sb.AppendLine($"{indent}public class {className}{baseClause}");
        sb.AppendLine($"{indent}{{");

        foreach (var member in dt.Members)
        {
            EmitProperty(sb, indent + "    ", member);
        }

        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private void EmitGvlClasses(StringBuilder sb, string indent)
    {
        foreach (var gvl in _model.Gvls)
        {
            string className = SanitizeIdentifier(gvl.Name);

            sb.AppendLine($"{indent}public class {className}");
            sb.AppendLine($"{indent}{{");

            foreach (var variable in gvl.Variables)
            {
                if (variable.IsConstant)
                    EmitConstant(sb, indent + "    ", variable);
                else
                    EmitProperty(sb, indent + "    ", variable, isVirtual: true);
            }

            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }
    }

    private void EmitProperty(StringBuilder sb, string memberIndent, ParsedVariable variable, bool isVirtual = false)
    {
        if (!string.IsNullOrWhiteSpace(variable.Documentation))
        {
            sb.AppendLine($"{memberIndent}/// <summary>");
            foreach (var line in SplitDocLines(variable.Documentation!))
                sb.AppendLine($"{memberIndent}/// {line}");
            sb.AppendLine($"{memberIndent}/// </summary>");
        }

        string csType = RenderType(variable.Type);
        string name = SanitizeIdentifier(variable.Name);
        string modifier = isVirtual ? "virtual " : string.Empty;
        string initializer = BuildPropertyInitializer(variable);
        sb.AppendLine($"{memberIndent}public {modifier}{csType} {name} {{ get; set; }}{initializer}");
    }

    private void EmitConstant(StringBuilder sb, string memberIndent, ParsedVariable variable)
    {
        if (!string.IsNullOrWhiteSpace(variable.Documentation))
        {
            sb.AppendLine($"{memberIndent}/// <summary>");
            foreach (var line in SplitDocLines(variable.Documentation!))
                sb.AppendLine($"{memberIndent}/// {line}");
            sb.AppendLine($"{memberIndent}/// </summary>");
        }

        string csType = RenderType(variable.Type);
        string name = SanitizeIdentifier(variable.Name);

        if (IsConstEligible(variable))
        {
            string literal = RenderInitialLiteral(variable.Type, variable.InitialValueLiteral!);
            sb.AppendLine($"{memberIndent}public const {csType} {name} = {literal};");
        }
        else
        {
            string initializer = BuildStaticReadonlyInitializer(variable);
            sb.AppendLine($"{memberIndent}public static readonly {csType} {name}{initializer};");
        }
    }

    private void EmitProxyClass(StringBuilder sb, string indent, string gvlName, IReadOnlyList<ParsedVariable> proxyVars)
    {
        string gvlClassName = SanitizeIdentifier(gvlName);
        string proxyClassName = gvlClassName + "Proxy";
        string memberIndent = indent + "    ";

        sb.AppendLine($"{indent}/// <inheritdoc />");
        sb.AppendLine($"{indent}public class {proxyClassName} : {gvlClassName}");
        sb.AppendLine($"{indent}{{");

        sb.AppendLine($"{memberIndent}private readonly {_config.PlcControlTypeName} _plcControl;");
        sb.AppendLine($"{memberIndent}private readonly {gvlClassName} _model;");
        sb.AppendLine($"{memberIndent}private DateTime _lastRead;");
        sb.AppendLine($"{memberIndent}private readonly TimeSpan _updateInterval;");
        sb.AppendLine();

        sb.AppendLine($"{memberIndent}public {proxyClassName}({_config.HardwareControlPoolTypeName} hardwareControl)");
        sb.AppendLine($"{memberIndent}{{");
        sb.AppendLine($"{memberIndent}    _plcControl = hardwareControl.PlcControl;");
        sb.AppendLine($"{memberIndent}    _model = new {gvlClassName}();");
        sb.AppendLine($"{memberIndent}    _lastRead = DateTime.MinValue;");
        sb.AppendLine($"{memberIndent}    _updateInterval = TimeSpan.FromMilliseconds(500);");
        sb.AppendLine($"{memberIndent}}}");

        foreach (var variable in proxyVars)
            EmitProxyProperty(sb, memberIndent, gvlName, variable);

        EmitProxyReadValues(sb, memberIndent, gvlName, proxyVars);

        sb.AppendLine();
        sb.AppendLine($"{memberIndent}private bool IsUpdateRequired()");
        sb.AppendLine($"{memberIndent}{{");
        sb.AppendLine($"{memberIndent}    return DateTime.Now - _lastRead > _updateInterval;");
        sb.AppendLine($"{memberIndent}}}");

        sb.AppendLine($"{indent}}}");
    }

    private void EmitProxyProperty(StringBuilder sb, string memberIndent, string gvlName, ParsedVariable variable)
    {
        string name = SanitizeIdentifier(variable.Name);
        string csType = RenderType(variable.Type);

        // Non-scalar variables (structs, arrays, pointers) can't be read with a
        // single generic call, so leave them to the inherited virtual default.
        if (!IsProxyReadable(variable.Type))
        {
            sb.AppendLine();
            sb.AppendLine($"{memberIndent}// {name}: type '{csType}' is not a primitive scalar.");
            sb.AppendLine($"{memberIndent}//   Node: {gvlName}.{variable.Name}");
            sb.AppendLine($"{memberIndent}//   Override manually (struct / array reads need framework-specific calls).");
            return;
        }

        sb.AppendLine();
        sb.AppendLine($"{memberIndent}/// <inheritdoc />");
        sb.AppendLine($"{memberIndent}public override {csType} {name}");
        sb.AppendLine($"{memberIndent}{{");
        sb.AppendLine($"{memberIndent}    get");
        sb.AppendLine($"{memberIndent}    {{");
        sb.AppendLine($"{memberIndent}        ReadValues();");
        sb.AppendLine($"{memberIndent}        return _model.{name};");
        sb.AppendLine($"{memberIndent}    }}");
        sb.AppendLine($"{memberIndent}}}");
    }

    private void EmitProxyReadValues(StringBuilder sb, string memberIndent, string gvlName, IReadOnlyList<ParsedVariable> proxyVars)
    {
        sb.AppendLine();
        sb.AppendLine($"{memberIndent}private void ReadValues()");
        sb.AppendLine($"{memberIndent}{{");
        sb.AppendLine($"{memberIndent}    if (!IsUpdateRequired()) return;");
        sb.AppendLine();

        foreach (var variable in proxyVars)
        {
            if (!IsProxyReadable(variable.Type))
                continue;

            string name = SanitizeIdentifier(variable.Name);
            string nodePath = $"{gvlName}.{variable.Name}";
            string readType = RenderType(variable.Type);
            string resultVar = ToCamelCase(name) + "Result";

            sb.AppendLine($"{memberIndent}    var {resultVar} = _plcControl.{_config.PlcReadMethodName}<{readType}>(\"{EscapeString(nodePath)}\");");
            if (variable.Type is StringType)
                sb.AppendLine($"{memberIndent}    _model.{name} = {resultVar}.IsSuccess ? ({resultVar}.Value ?? string.Empty) : _model.{name};");
            else
                sb.AppendLine($"{memberIndent}    _model.{name} = {resultVar}.IsSuccess ? {resultVar}.Value : _model.{name};");
        }

        sb.AppendLine();
        sb.AppendLine($"{memberIndent}    _lastRead = DateTime.Now;");
        sb.AppendLine($"{memberIndent}}}");
    }

    private bool IsProxyReadable(ParsedType type)
        => type is PrimitiveType or StringType
           || (type is DerivedType d && _enumTypeNames.Contains(d.Name));

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "value";
        if (name.Length == 1)
            return name.ToLowerInvariant();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static IReadOnlyDictionary<string, int> BuildConstantLookup(ParsedModel model)
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var gvl in model.Gvls)
        {
            foreach (var v in gvl.Variables)
            {
                if (!v.IsConstant || v.InitialValueLiteral == null)
                    continue;

                if (TryParseIntLiteral(v.InitialValueLiteral, out int value))
                    dict[v.Name] = value;
            }
        }
        return dict;
    }

    private string RenderType(ParsedType type)
    {
        return type switch
        {
            PrimitiveType p => p.CsName,
            StringType => "string",
            DerivedType d => SanitizeIdentifier(d.Name),
            PointerType p => "System.IntPtr",
            ArrayType a => $"{RenderType(a.ElementType)}[]",
            UnknownType => "object",
            _ => "object"
        };
    }

    private void CollectDerivedNames(ParsedType type, HashSet<string> set)
    {
        switch (type)
        {
            case DerivedType d:
                set.Add(d.Name);
                break;
            case ArrayType a:
                CollectDerivedNames(a.ElementType, set);
                break;
            case PointerType p:
                CollectDerivedNames(p.BaseType, set);
                break;
        }
    }

    private string BuildPropertyInitializer(ParsedVariable variable)
    {
        switch (variable.Type)
        {
            case StringType:
                return " = string.Empty;";
            case ArrayType arr:
                {
                    string elementType = RenderType(arr.ElementType);
                    if (TryResolveArraySize(arr, out int size))
                        return $" = new {elementType}[{size}];";
                    return $" = System.Array.Empty<{elementType}>();";
                }
            case DerivedType d:
                // Enums are value types coming from an external namespace: no initializer.
                return _enumTypeNames.Contains(d.Name)
                    ? string.Empty
                    : $" = new {SanitizeIdentifier(d.Name)}();";
        }

        if (variable.InitialValueLiteral != null && IsScalarType(variable.Type))
        {
            string literal = RenderInitialLiteral(variable.Type, variable.InitialValueLiteral);
            return $" = {literal};";
        }

        return string.Empty;
    }

    private string BuildStaticReadonlyInitializer(ParsedVariable variable)
    {
        switch (variable.Type)
        {
            case StringType:
                return " = string.Empty";
            case ArrayType arr:
                {
                    string elementType = RenderType(arr.ElementType);
                    if (TryResolveArraySize(arr, out int size))
                        return $" = new {elementType}[{size}]";
                    return $" = System.Array.Empty<{elementType}>()";
                }
            case DerivedType d:
                return _enumTypeNames.Contains(d.Name)
                    ? string.Empty
                    : $" = new {SanitizeIdentifier(d.Name)}()";
        }

        if (variable.InitialValueLiteral != null && IsScalarType(variable.Type))
        {
            string literal = RenderInitialLiteral(variable.Type, variable.InitialValueLiteral);
            return $" = {literal}";
        }

        return string.Empty;
    }

    private bool TryResolveArraySize(ArrayType array, out int size)
    {
        size = 0;
        if (!TryResolveBound(array.LowerBound, out int lower))
            return false;
        if (!TryResolveBound(array.UpperBound, out int upper))
            return false;

        size = Math.Max(0, upper - lower + 1);
        return true;
    }

    private bool TryResolveBound(string bound, out int value)
    {
        if (TryParseIntLiteral(bound, out value))
            return true;
        if (_constantLookup.TryGetValue(bound, out value))
            return true;
        value = 0;
        return false;
    }

    private static bool IsConstEligible(ParsedVariable variable)
    {
        if (variable.InitialValueLiteral == null)
            return false;

        return variable.Type switch
        {
            PrimitiveType p => p.CsName is "bool" or "byte" or "sbyte" or "short" or "ushort"
                                          or "int" or "uint" or "long" or "ulong"
                                          or "float" or "double",
            StringType => true,
            _ => false
        };
    }

    private static bool IsScalarType(ParsedType type)
        => type is PrimitiveType or StringType;

    private static string RenderInitialLiteral(ParsedType type, string rawValue)
    {
        string value = rawValue.Trim();

        if (type is StringType)
        {
            // Strip leading/trailing single quotes used by ST syntax.
            if (value.Length >= 2 && value.StartsWith("'", StringComparison.Ordinal) && value.EndsWith("'", StringComparison.Ordinal))
                value = value[1..^1];
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        if (type is PrimitiveType prim)
        {
            if (prim.CsName == "bool")
                return value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ? "true" : "false";

            // Hex notation: 16#7FFF -> 0x7FFF
            var hexMatch = Regex.Match(value, "^16#([0-9A-Fa-f]+)$");
            if (hexMatch.Success)
                return "0x" + hexMatch.Groups[1].Value;

            if (prim.CsName == "float")
                return value.Contains('.', StringComparison.Ordinal) ? value + "f" : value;

            return value;
        }

        return value;
    }

    private static bool TryParseIntLiteral(string raw, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string trimmed = raw.Trim();

        var hexMatch = Regex.Match(trimmed, "^16#([0-9A-Fa-f]+)$");
        if (hexMatch.Success)
            return int.TryParse(hexMatch.Groups[1].Value,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);

        return int.TryParse(trimmed,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out value);
    }

    private static IEnumerable<string> SplitDocLines(string text)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            string line = rawLine.Replace("\r", string.Empty).Trim();
            if (line.Length > 0)
                yield return line;
        }
    }

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

    private static string EscapeString(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
