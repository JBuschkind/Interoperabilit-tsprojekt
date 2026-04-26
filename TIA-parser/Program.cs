using TiaPortalParser;

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    CliUtils.PrintUsage();
    return;
}

var positionalArgs = CliArgsParser.ParsePositionalArgs(args);
var config = CliArgsParser.ParseConfigArgs(args);

if (positionalArgs.Count == 0 || positionalArgs.Count > 3 || config == null)
{
    CliUtils.PrintUsage();
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