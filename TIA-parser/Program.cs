using TiaPortalParser;

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintUsage();
    return;
}

var positionalArgs = new List<string>();
var config = new TiaCodeGeneratorConfig();

for (int index = 0; index < args.Length; index++)
{
    string arg = args[index];

    if (!arg.StartsWith("-", StringComparison.Ordinal))
    {
        positionalArgs.Add(arg);
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
            PrintUsage();
            return;
    }
}

if (positionalArgs.Count == 0 || positionalArgs.Count > 3)
{
    PrintUsage();
    return;
}

string inputPath = positionalArgs[0];
string outputSpsPath = "Sps.cs";
string outputProxyPath = "SpsProxy.cs";

if(positionalArgs.Count == 2) {
    outputSpsPath = positionalArgs[1] + "/Sps.cs";
    outputProxyPath = positionalArgs[1] + "/SpsProxy.cs";
}
else if(positionalArgs.Count == 3) {
    outputSpsPath = positionalArgs[1];
    outputProxyPath = positionalArgs[2];
}


TiaDataBlock dataBlock = TiaPortalDbParser.ParseFile(inputPath);

TiaCodeGenerator.GenerateFile(
    dataBlock.Variables,
    config,
    outputSpsPath
);

TiaProxyGenerator.GenerateFile(
    dataBlock,
    config,
    outputProxyPath
);

static string GetRequiredValue(string[] args, ref int index, string optionName)
{
    if (index + 1 >= args.Length)
        throw new ArgumentException($"Missing value for {optionName}.");

    index++;
    return args[index];
}

static List<string> ParseListValue(string value)
{
    return value
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
}

static bool ParseBoolValue(string value, string optionName)
{
    if (bool.TryParse(value, out bool parsed))
        return parsed;

    throw new ArgumentException($"Invalid boolean value '{value}' for {optionName}. Use 'true' or 'false'.");
}

static int ParseIntValue(string value, string optionName)
{
    if (int.TryParse(value, out int parsed))
        return parsed;

    throw new ArgumentException($"Invalid integer value '{value}' for {optionName}.");
}

static void PrintUsage()
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
