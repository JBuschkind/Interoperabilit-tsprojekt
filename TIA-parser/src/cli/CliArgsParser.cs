namespace TiaPortalParser {
    /// <summary>
    /// Parses CLI arguments into configuration options and positional parameters.
    /// </summary>
    public class CliArgsParser
    {
        /// <summary>
        /// Parses supported option arguments and returns a populated generator configuration.
        /// </summary>
        public static TiaCodeGeneratorConfig? ParseConfigArgs(string[] args)
        {
            var config = new TiaCodeGeneratorConfig();

            for (int index = 0; index < args.Length; index++)
            {
                string arg = args[index];

                if (!arg.StartsWith("-", StringComparison.Ordinal))
                {
                    continue;
                }

                switch (arg)
                {
                    case "--namespace":
                        config.Namespace = GetRequiredValue(args, ref index, arg);
                        break;
                    case "--class-name":
                        config.ClassName = GetRequiredValue(args, ref index, arg);
                        break;
                    case "--using":
                        config.AdditionalUsings.AddRange(ParseListValue(GetRequiredValue(args, ref index, arg)));
                        break;
                    case "--proxy-using":
                        config.AdditionalProxyUsings.AddRange(ParseListValue(GetRequiredValue(args, ref index, arg)));
                        break;
                    case "--use-virtual-properties":
                        config.UseVirtualProperties = ParseBoolValue(GetRequiredValue(args, ref index, arg), arg);
                        break;
                    case "--namespace-id":
                        config.NamespaceId = ParseIntValue(GetRequiredValue(args, ref index, arg), arg);
                        break;
                    case "--update-interval-ms":
                        config.UpdateIntervalMs = ParseIntValue(GetRequiredValue(args, ref index, arg), arg);
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown argument: {arg}");
                        return null;
                }
            }

            return config;
        }

        /// <summary>
        /// Returns positional arguments before the first option argument.
        /// </summary>
        public static List<string> ParsePositionalArgs(string[] args)
        {
            var positionalArgs = new List<string>();

            for (int index = 0; index < args.Length; index++)
            {
                if (args[index].StartsWith("-", StringComparison.Ordinal))
                {
                    return positionalArgs;
                }
                positionalArgs.Add(args[index]);
            }
            return positionalArgs;
        }

        private static string GetRequiredValue(string[] args, ref int index, string optionName)
        {
            if (index + 1 >= args.Length)
                throw new ArgumentException($"Missing value for {optionName}.");

            index++;
            return args[index];
        }

        private static List<string> ParseListValue(string value)
        {
            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        private static bool ParseBoolValue(string value, string optionName)
        {
            if (bool.TryParse(value, out bool parsed))
                return parsed;

            throw new ArgumentException($"Invalid boolean value '{value}' for {optionName}. Use 'true' or 'false'.");
        }

        private static int ParseIntValue(string value, string optionName)
        {
            if (int.TryParse(value, out int parsed))
                return parsed;

            throw new ArgumentException($"Invalid integer value '{value}' for {optionName}.");
        }
    }
}