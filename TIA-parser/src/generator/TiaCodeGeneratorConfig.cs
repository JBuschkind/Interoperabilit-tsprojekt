namespace TiaPortalParser
{
    /// <summary>
    /// Configuration for the C# class file that the generator produces.
    /// </summary>
    public class TiaCodeGeneratorConfig
    {
        /// <summary>Namespace written at the top of the file, e.g. "OPT.Framework.MyProject.Hardware".</summary>
        public string Namespace { get; set; } = "OPT.Framework.MyProject.Hardware";

        /// <summary>Class name, e.g. "Sps".</summary>
        public string ClassName { get; set; } = "Sps";

        /// <summary>
        /// Optional extra using-directives (one per entry, without the 'using' keyword or semicolon).
        /// The ReSharper suppression comment and the class summary are always emitted.
        /// </summary>
        public List<string> AdditionalUsings { get; set; } = new();

        /// <summary>
        /// Optional extra using-directives (one per entry, without the 'using' keyword or semicolon).
        /// The ReSharper suppression comment and the class summary are always emitted.
        /// </summary>
        public List<string> AdditionalProxyUsings { get; set; } = new();

        /// <summary>
        /// When true, each property is emitted as 'public virtual T Name'.
        /// When false, 'public T Name' is used instead.
        /// </summary>
        public bool UseVirtualProperties { get; set; } = true;

        /// <summary>
        /// Namespace index to use for OPC UA NodeIds.
        /// </summary>
        public int NamespaceId { get; set; } = 3;

        /// <summary>
        /// Update interval in milliseconds for the generated proxy class. Determines how often the proxy reads values from the OPC UA server.
        /// Default is 500 ms.
        /// <summary>
        public int UpdateIntervalMs { get; set; } = 500;
    }
}
