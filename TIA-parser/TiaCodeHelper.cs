using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TiaPortalParser
{
    /// <summary>
    /// Shared helper functions for code generation (type mapping, naming, escaping).
    /// </summary>
    public static class TiaCodeHelper
    {
        // ── TIA Portal → C# type map ────────────────────────────────────────

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

        /// <summary>
        /// Maps a TIA Portal data type string to the closest C# primitive.
        /// Unknown types are returned as-is so the user can resolve them manually.
        /// </summary>
        public static string MapType(string tiaType)
        {
            if (TypeMap.TryGetValue(tiaType, out string? mapped))
                return mapped;

            // Array types, e.g. "Array[0..9] of Real"
            if (tiaType.StartsWith("Array", StringComparison.OrdinalIgnoreCase))
                return $"object /* {tiaType} */";

            return tiaType;
        }

        // ── Name conversion ─────────────────────────────────────────────────

        /// <summary>
        /// Converts a TIA Portal variable name to a valid PascalCase C# identifier.
        /// </summary>
        public static string ToPascalCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "_Unknown";

            name = NormalizeGermanCharacters(name);

            string[] parts = Regex.Split(name, @"[^A-Za-z0-9]+");

            var sb = new StringBuilder();
            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                    sb.Append(part[1..]);
            }

            string result = sb.ToString();

            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;

            return result;
        }

        private static string NormalizeGermanCharacters(string input)
        {
            return input
                .Replace("ä", "ae")
                .Replace("Ä", "Ae")
                .Replace("ö", "oe")
                .Replace("Ö", "Oe")
                .Replace("ü", "ue")
                .Replace("Ü", "Ue")
                .Replace("ß", "ss");
        }

        /// <summary>
        /// Escapes characters that are invalid inside an XML doc comment.
        /// </summary>
        public static string EscapeXmlComment(string text) =>
            text.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
    }
}