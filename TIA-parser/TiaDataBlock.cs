namespace TiaPortalParser
{
    /// <summary>
    /// Represents a collection of variables parsed from a TIA Portal .db file.
    /// </summary>
    public class TiaDataBlock
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>The list of parsed variables.</summary>
        public List<TiaVariable> Variables { get; set; } = new List<TiaVariable>();

        public override string ToString() =>
            $"TiaVariableList with {Variables.Count} variables";
    }
}