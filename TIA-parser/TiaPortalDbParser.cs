using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TiaPortalParser
{
    /// <summary>
    /// Represents a single variable parsed from a TIA Portal .db file.
    /// </summary>
    public class TiaVariable
    {
        /// <summary>The variable name as declared (without surrounding quotes).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The data type (e.g. Bool, Int, Struct).</summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>The inline comment (text after //).</summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// True if the variable carries { ExternalWritable := 'True' },
        /// false if it carries 'False', and null if the attribute is absent.
        /// </summary>
        public bool? ExternalWritable { get; set; }

        /// <summary>
        /// Dot-separated path of enclosing struct names, e.g. "SPS -> PC" or
        /// "PC -> SPS". Empty string for top-level variables.
        /// </summary>
        public string StructPath { get; set; } = string.Empty;

        /// <summary>Full path including the variable name itself.</summary>
        public string FullPath => string.IsNullOrEmpty(StructPath)
            ? Name
            : $"{StructPath}.{Name}";

        public override string ToString() =>
            $"[{FullPath}]  Type={DataType}  ExternalWritable={ExternalWritable?.ToString() ?? "n/a"}  Comment={Comment}";
    }

    /// <summary>
    /// Parses TIA Portal Data Block (.db) files into strongly-typed <see cref="TiaVariable"/> objects.
    /// </summary>
    public static class TiaPortalDbParser
    {
        // Matches the optional attribute block, e.g. { ExternalWritable := 'False' }
        private static readonly Regex AttributeRegex = new Regex(
            @"\{\s*ExternalWritable\s*:=\s*'(?<val>True|False)'\s*\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches a quoted name like "SPS -> PC" or an unquoted identifier
        private static readonly Regex NameRegex = new Regex(
            @"^(?:""(?<qname>[^""]+)""|(?<uname>[A-Za-z_][A-Za-z0-9_]*))",
            RegexOptions.Compiled);

        // Matches the ': <DataType>' part at the end of the declaration portion
        private static readonly Regex TypeRegex = new Regex(
            @":\s*(?<type>[A-Za-z_][A-Za-z0-9_\[\]\.]*)\s*[;{]?",
            RegexOptions.Compiled);

        /// <summary>
        /// Parses a .db file from disk and returns every declared variable.
        /// Struct declarations themselves are not returned — only leaf variables.
        /// </summary>
        public static List<TiaVariable> ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("DB file not found.", filePath);

            string content = File.ReadAllText(filePath, Encoding.UTF8);
            return Parse(content);
        }

        /// <summary>
        /// Parses the raw text of a .db file and returns every declared variable.
        /// </summary>
        public static List<TiaVariable> Parse(string content)
        {
            var results = new List<TiaVariable>();
            var structStack = new Stack<string>(); // tracks current struct nesting path

            bool inVarBlock = false;

            foreach (string rawLine in content.Split('\n'))
            {
                string line = rawLine.Trim();

                // ── Block boundaries ─────────────────────────────────────
                if (line.StartsWith("VAR", StringComparison.OrdinalIgnoreCase))
                {
                    inVarBlock = true;
                    continue;
                }

                if (line.StartsWith("END_VAR", StringComparison.OrdinalIgnoreCase))
                {
                    inVarBlock = false;
                    structStack.Clear();
                    continue;
                }

                if (!inVarBlock)
                    continue;

                // ── Struct open ───────────────────────────────────────────
                // e.g.   "SPS -> PC" { ExternalWritable := 'False'} : Struct
                if (IsStructDeclaration(line))
                {
                    string structName = ExtractName(line);
                    structStack.Push(structName);
                    continue;
                }

                // ── Struct close ──────────────────────────────────────────
                if (line.StartsWith("END_STRUCT", StringComparison.OrdinalIgnoreCase))
                {
                    if (structStack.Count > 0)
                        structStack.Pop();
                    continue;
                }

                // ── Variable declaration ──────────────────────────────────
                TiaVariable? variable = TryParseVariable(line, structStack);
                if (variable != null)
                    results.Add(variable);
            }

            return results;
        }

        // ── Helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the line ends with ': Struct' (after stripping attributes and comments).
        /// </summary>
        private static bool IsStructDeclaration(string line)
        {
            // Remove attribute block and inline comment before checking
            string stripped = AttributeRegex.Replace(line, string.Empty);
            int commentPos = stripped.IndexOf("//", StringComparison.Ordinal);
            if (commentPos >= 0)
                stripped = stripped[..commentPos];

            return Regex.IsMatch(stripped.Trim(), @":\s*Struct\s*;?\s*$", RegexOptions.IgnoreCase);
        }

        /// <summary>Extracts the variable/struct name from a declaration line.</summary>
        private static string ExtractName(string line)
        {
            Match m = NameRegex.Match(line.TrimStart());
            if (!m.Success) return string.Empty;

            return m.Groups["qname"].Success
                ? m.Groups["qname"].Value
                : m.Groups["uname"].Value;
        }

        /// <summary>
        /// Tries to parse a leaf variable declaration from a single line.
        /// Returns null if the line is not a valid variable declaration.
        /// </summary>
        private static TiaVariable? TryParseVariable(string line, Stack<string> structStack)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            // ── 1. Extract inline comment ─────────────────────────────
            string comment = string.Empty;
            int commentIdx = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIdx >= 0)
            {
                comment = line[(commentIdx + 2)..].Trim();
                line = line[..commentIdx].Trim();
            }

            // ── 2. Extract ExternalWritable attribute ─────────────────
            bool? externalWritable = null;
            Match attrMatch = AttributeRegex.Match(line);
            if (attrMatch.Success)
            {
                externalWritable = attrMatch.Groups["val"].Value
                    .Equals("True", StringComparison.OrdinalIgnoreCase);
                line = AttributeRegex.Replace(line, string.Empty).Trim();
            }

            // ── 3. Extract name ───────────────────────────────────────
            string name = ExtractName(line);
            if (string.IsNullOrEmpty(name))
                return null;

            // ── 4. Extract data type (must end in ';') ─────────────────
            // The declaration must end with a semicolon to be a proper variable line
            if (!line.TrimEnd().EndsWith(";"))
                return null;

            Match typeMatch = TypeRegex.Match(line);
            if (!typeMatch.Success)
                return null;

            string dataType = typeMatch.Groups["type"].Value;

            // Ignore 'Struct' type here — handled separately as a container
            if (dataType.Equals("Struct", StringComparison.OrdinalIgnoreCase))
                return null;

            // ── 5. Build struct path ──────────────────────────────────
            string structPath = BuildPath(structStack);

            return new TiaVariable
            {
                Name = name,
                DataType = dataType,
                Comment = comment,
                ExternalWritable = externalWritable,
                StructPath = structPath,
            };
        }

        /// <summary>Converts the struct stack (LIFO) into a dot-separated path string.</summary>
        private static string BuildPath(Stack<string> stack)
        {
            if (stack.Count == 0)
                return string.Empty;

            // Stack is LIFO — reverse to get outermost-first order
            var segments = new List<string>(stack);
            segments.Reverse();
            return string.Join(".", segments);
        }
    }
}
