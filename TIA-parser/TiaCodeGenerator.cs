using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TiaPortalParser
{
    /// <summary>
    /// Configuration for the C# class file that the generator produces.
    /// </summary>
    public class CodeGeneratorConfig
    {
        /// <summary>Namespace written at the top of the file, e.g. "OPT.Framework.MyProject.Hardware".</summary>
        public string Namespace { get; set; } = "MyProject.Hardware";

        /// <summary>Class name, e.g. "Sps".</summary>
        public string ClassName { get; set; } = "Sps";

        /// <summary>
        /// Optional extra using-directives (one per entry, without the 'using' keyword or semicolon).
        /// The ReSharper suppression comment and the class summary are always emitted.
        /// </summary>
        public List<string> AdditionalUsings { get; set; } = new();

        /// <summary>
        /// When true, each property is emitted as 'public virtual T Name'.
        /// When false, 'public T Name' is used instead.
        /// </summary>
        public bool UseVirtualProperties { get; set; } = true;
    }

    /// <summary>
    /// Generates a C# class file from a list of <see cref="TiaVariable"/> objects.
    /// </summary>
    public static class TiaCodeGenerator
    {
        // ── TIA Portal → C# type map ──────────────────────────────────────────
        private static readonly Dictionary<string, string> TypeMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Bool"]   = "bool",
                ["Byte"]   = "byte",
                ["Word"]   = "ushort",
                ["DWord"]  = "uint",
                ["LWord"]  = "ulong",
                ["SInt"]   = "sbyte",
                ["USInt"]  = "byte",
                ["Int"]    = "short",
                ["UInt"]   = "ushort",
                ["DInt"]   = "int",
                ["UDInt"]  = "uint",
                ["LInt"]   = "long",
                ["ULInt"]  = "ulong",
                ["Real"]   = "float",
                ["LReal"]  = "double",
                ["String"] = "string",
                ["WString"]= "string",
                ["Char"]   = "char",
                ["Time"]   = "TimeSpan",
                ["Date"]   = "DateTime",
                ["TOD"]    = "TimeSpan",
                ["DTL"]    = "DateTime",
            };

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a C# source file and writes it to <paramref name="outputPath"/>.
        /// </summary>
        public static void GenerateFile(
            IEnumerable<TiaVariable> variables,
            CodeGeneratorConfig config,
            string outputPath)
        {
            string content = Generate(variables, config);
            File.WriteAllText(outputPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        /// <summary>
        /// Generates and returns the C# source as a string.
        /// </summary>
        public static string Generate(
            IEnumerable<TiaVariable> variables,
            CodeGeneratorConfig config)
        {
            var sb = new StringBuilder();

            AppendHeader(sb, config);
            AppendNamespaceOpen(sb, config);
            AppendClassOpen(sb, config);
            AppendConstructor(sb, config);
            AppendProperties(sb, variables, config);
            AppendClassClose(sb);
            AppendNamespaceClose(sb);

            return sb.ToString();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static void AppendHeader(StringBuilder sb, CodeGeneratorConfig config)
        {
            // Additional using directives
            foreach (string u in config.AdditionalUsings)
                sb.AppendLine($"using {u};");

            if (config.AdditionalUsings.Count > 0)
                sb.AppendLine();

            // ReSharper suppression + alternative comment
            sb.AppendLine("// ReSharper disable UnusedAutoPropertyAccessor.Global");
            sb.AppendLine();
            sb.AppendLine();
        }

        private static void AppendNamespaceOpen(StringBuilder sb, CodeGeneratorConfig config)
        {
            sb.AppendLine($"namespace {config.Namespace}");
            sb.AppendLine("{");
        }

        private static void AppendClassOpen(StringBuilder sb, CodeGeneratorConfig config)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Variables definition");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public class {config.ClassName}");
            sb.AppendLine("    {");
        }

        private static void AppendConstructor(StringBuilder sb, CodeGeneratorConfig config)
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Ctor");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public {config.ClassName}()");
            sb.AppendLine("        {");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void AppendProperties(
            StringBuilder sb,
            List<TiaVariable> vars,
            CodeGeneratorConfig config)
        {
            for (int i = 0; i < vars.Count; i++)
            {
                TiaVariable v = vars[i];

                string csType = MapType(v.DataType);
                string propName = ToPascalCase(v.Name);
                string modifier = config.UseVirtualProperties ? "virtual " : string.Empty;

                // XML doc comment
                if (!string.IsNullOrWhiteSpace(v.Comment))
                {
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// {EscapeXmlComment(v.Comment)}");
                    sb.AppendLine("        /// </summary>");
                }

                sb.AppendLine($"        public {modifier}{csType} {propName} {{ get; set; }}");

                // Blank line between properties, not after the last one
                if (i < vars.Count - 1)
                    sb.AppendLine();
            }
        }

        private static void AppendClassClose(StringBuilder sb)
        {
            sb.AppendLine("    }");
        }

        private static void AppendNamespaceClose(StringBuilder sb)
        {
            sb.AppendLine("}");
        }

        // ── Type mapping ──────────────────────────────────────────────────────

        /// <summary>
        /// Maps a TIA Portal data type string to the closest C# primitive.
        /// Unknown types are returned as-is so the user can resolve them manually.
        /// </summary>
        private static string MapType(string tiaType)
        {
            if (TypeMap.TryGetValue(tiaType, out string? mapped))
                return mapped;

            // Array types, e.g. "Array[0..9] of Real" — return as comment placeholder
            if (tiaType.StartsWith("Array", StringComparison.OrdinalIgnoreCase))
                return "object /* " + tiaType + " */";

            return tiaType; // keep unknown types verbatim
        }

        // ── Name conversion ───────────────────────────────────────────────────

        /// <summary>
        /// Converts a TIA Portal variable name to a valid PascalCase C# identifier.
        /// E.g.  "SPS an"          →  "SpsAn"
        ///       "N-H 1-4 quitt"   →  "NH14Quitt"
        ///       "Bus nio Amada"   →  "BusNioAmada"
        ///       "UserLevel1Active" → "UserLevel1Active"  (already clean)
        /// </summary>
        private static string ToPascalCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "_Unknown";

            // Split on any non-alphanumeric character; keep numeric segments
            string[] parts = Regex.Split(name, @"[^A-Za-z0-9]+");

            var sb = new StringBuilder();
            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                // Capitalise first letter, keep the rest as-is
                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                    sb.Append(part[1..]);
            }

            string result = sb.ToString();

            // Ensure the identifier does not start with a digit
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;

            return result;
        }

        /// <summary>Escapes characters that are invalid inside an XML doc comment.</summary>
        private static string EscapeXmlComment(string text) =>
            text.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
    }
}
