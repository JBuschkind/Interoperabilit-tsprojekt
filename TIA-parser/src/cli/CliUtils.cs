namespace TiaPortalParser
{
    /// <summary>
    /// Provides shared helper methods for CLI output.
    /// </summary>
    public class CliUtils
    {
        /// <summary>
        /// Prints command usage and available CLI options to the console.
        /// </summary>
        public static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  tiaparser <inputDbPath> [outputSpsPath] [outputProxyPath] [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --namespace <value>");
            Console.WriteLine("  --class-name <value>");
            Console.WriteLine("  --using <ns1,ns2,...>");
            Console.WriteLine("  --proxy-using <ns1,ns2,...>");
            Console.WriteLine("  --use-virtual-properties <true|false>");
            Console.WriteLine("  --namespace-id <int>");
            Console.WriteLine("  --update-interval-ms <int>");
        }
    }
}