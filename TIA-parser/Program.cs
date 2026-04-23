using TiaPortalParser;

List<TiaVariable> variables = TiaPortalDbParser.ParseFile("resources/Schnittstelle SPS - PC.db");

foreach (TiaVariable v in variables)
{
    Console.WriteLine(v.FullPath);
    Console.WriteLine($"  Type:             {v.DataType}");
    Console.WriteLine($"  ExternalWritable: {v.ExternalWritable}");
    Console.WriteLine($"  Comment:          {v.Comment}");
    Console.WriteLine();
}