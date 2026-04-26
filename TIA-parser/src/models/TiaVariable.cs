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

        /// <summary>
        /// Returns a readable representation of the variable for debugging and logging.
        /// </summary>
        public override string ToString() =>
            $"[{FullPath}]  Type={DataType}  ExternalWritable={ExternalWritable?.ToString() ?? "n/a"}  Comment={Comment}";
    }
}