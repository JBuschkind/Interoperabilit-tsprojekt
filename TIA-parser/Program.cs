using TiaPortalParser;

TiaDataBlock dataBlock = TiaPortalDbParser.ParseFile("input/Schnittstelle SPS - PC.db");

Console.WriteLine($"Parsed data block: {dataBlock.Name}");

var config = new CodeGeneratorConfig();

TiaCodeGenerator.GenerateFile(
    dataBlock.Variables,
    config,
    "output/Sps.cs"
);