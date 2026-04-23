using TiaPortalParser;

if (args.Length > 3 || args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run [inputDbPath] [outputSpsPath] [outputProxyPath]");
    return;
}

string inputPath = args[0];
string outputSpsPath = args.Length > 1 ? args[1] : "Sps.cs";
string outputProxyPath = args.Length > 2 ? args[2] : "SpsProxy.cs";

TiaDataBlock dataBlock = TiaPortalDbParser.ParseFile(inputPath);

Console.WriteLine($"Parsed data block: {dataBlock.Name}");

var config = new TiaCodeGeneratorConfig();

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
