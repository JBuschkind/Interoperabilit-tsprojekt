using System.Text;
using System.Text.RegularExpressions;

namespace AmlParser.Modular.Service;

internal sealed class GvlCodeGenerator
{
    private readonly ParsedModel _model;
    private readonly PlcStatusControlConfig _config;
    private readonly IReadOnlyDictionary<string, int> _constantLookup;
    private readonly HashSet<string> _definedDataTypeNames;

    public GvlCodeGenerator(ParsedModel model, PlcStatusControlConfig config)
    {
        _model = model;
        _config = config;
        _constantLookup = BuildConstantLookup(model);
        _definedDataTypeNames = new HashSet<string>(
            model.DataTypes.Select(d => d.Name),
            StringComparer.Ordinal);
    }

    public string EmitGvlFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
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
        sb.AppendLine();

        bool hasNamespace = !string.IsNullOrWhiteSpace(_config.Namespace);
        if (hasNamespace)
        {
            sb.AppendLine($"namespace {_config.Namespace}");
            sb.AppendLine("{");
        }

        string indent = hasNamespace ? "    " : string.Empty;
        string proxyClassName = SanitizeIdentifier(string.IsNullOrWhiteSpace(_config.ClassName)
            ? "GvlProxy"
            : _config.ClassName);

        sb.AppendLine($"{indent}public class {proxyClassName}");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    private readonly {_config.PlcControlTypeName} _plcControl;");
        sb.AppendLine();
        sb.AppendLine($"{indent}    public {proxyClassName}({_config.HardwareControlPoolTypeName} hardwareControl)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        _plcControl = hardwareControl.PlcControl;");
        sb.AppendLine($"{indent}    }}");

        foreach (var gvl in _model.Gvls)
        {
            foreach (var variable in gvl.Variables)
            {
                if (variable.IsConstant)
                    continue;

                EmitProxyMethod(sb, indent, gvl.Name, variable);
            }
        }

        sb.AppendLine($"{indent}}}");

        if (hasNamespace)
            sb.AppendLine("}");

        return sb.ToString();
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
                    EmitProperty(sb, indent + "    ", variable);
            }

            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }
    }

    private void EmitProperty(StringBuilder sb, string memberIndent, ParsedVariable variable)
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
        string initializer = BuildPropertyInitializer(variable);
        sb.AppendLine($"{memberIndent}public {csType} {name} {{ get; set; }}{initializer}");
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

    private void EmitProxyMethod(StringBuilder sb, string indent, string gvlName, ParsedVariable variable)
    {
        string methodName = "Get" + SanitizeIdentifier(gvlName) + "_" + SanitizeIdentifier(variable.Name);
        string nodePath = $"{gvlName}.{variable.Name}";

        switch (variable.Type)
        {
            case PrimitiveType prim:
                {
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    public {prim.CsName} {methodName}()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        var result = _plcControl.{_config.PlcReadMethodName}<{prim.CsName}>(\"{EscapeString(nodePath)}\");");
                    sb.AppendLine($"{indent}        return result.IsSuccess ? result.Value : default;");
                    sb.AppendLine($"{indent}    }}");
                    break;
                }
            case StringType:
                {
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    public string {methodName}()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        var result = _plcControl.{_config.PlcReadMethodName}<string>(\"{EscapeString(nodePath)}\");");
                    sb.AppendLine($"{indent}        return result.IsSuccess ? (result.Value ?? string.Empty) : string.Empty;");
                    sb.AppendLine($"{indent}    }}");
                    break;
                }
            case DerivedType:
            case ArrayType:
            case PointerType:
            case UnknownType:
            default:
                {
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    // {methodName}: type '{RenderType(variable.Type)}' is not a primitive scalar.");
                    sb.AppendLine($"{indent}    //   Node: {nodePath}");
                    sb.AppendLine($"{indent}    //   Implement manually (struct / array reads need framework-specific calls).");
                    break;
                }
        }
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
                return $" = new {SanitizeIdentifier(d.Name)}();";
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
                return $" = new {SanitizeIdentifier(d.Name)}()";
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
