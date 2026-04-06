using AmlParser.Modular.Service;

namespace AmlParser.Modular.Controller;

public interface IParsingController
{
    /// <summary>
    /// Starts the generation pipeline based on the provided command-line arguments.
    /// </summary>
    /// <param name="args">Arguments for input and output paths.</param>
    /// <returns>0 when processing succeeds; otherwise 1.</returns>
    int Run(string[] args);
}

public sealed class ParsingController : IParsingController
{
    private readonly IGvlXmlService _gvlXmlService;

    /// <summary>
    /// Initializes the controller with the service that performs the XML processing.
    /// </summary>
    /// <param name="gvlXmlService">Service used for parsing and file generation.</param>
    public ParsingController(IGvlXmlService gvlXmlService)
    {
        _gvlXmlService = gvlXmlService;
    }

    /// <summary>
    /// Reads arguments, validates the input path, and orchestrates generation of
    /// PlcStatusControl and the variable list.
    /// </summary>
    /// <param name="args">CLI arguments in the order: XML, C# output, TXT output, properties.</param>
    /// <returns>0 on success; otherwise 1.</returns>
    public int Run(string[] args)
    {
        string projectRoot = ResolveProjectRoot();
        string inputFile = Path.Combine(projectRoot, "Input", "GVL_PLC.xml");
        string outputPlcStatusControlFile = Path.Combine(projectRoot, "Output", "PlcStatusControl.generated.cs");
        string outputVariableListFile = Path.Combine(projectRoot, "Output", "extracted_variables.txt");
        string propertiesFile = Path.Combine(projectRoot, "Input", "plcstatus.properties");

        if (args.Length >= 1 && !string.IsNullOrWhiteSpace(args[0]))
            inputFile = args[0];

        if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
            outputPlcStatusControlFile = args[1];

        if (args.Length >= 3 && !string.IsNullOrWhiteSpace(args[2]))
            outputVariableListFile = args[2];

        if (args.Length >= 4 && !string.IsNullOrWhiteSpace(args[3]))
            propertiesFile = args[3];

        if (args.Length > 4)
            Console.WriteLine("Hinweis: Ueberzaehlige Argumente werden ignoriert.");

        Console.WriteLine($"Eingabe XML:                    {inputFile}");
        Console.WriteLine($"Ausgabe PlcStatusControl:       {outputPlcStatusControlFile}");
        Console.WriteLine($"Ausgabe Variablenliste (TXT):   {outputVariableListFile}");
        Console.WriteLine($"Konfiguration (properties):     {propertiesFile}");

        if (!inputFile.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Fehler: Es werden nur XML-Dateien unterstützt.");
            return 1;
        }

        try
        {
            _gvlXmlService.GeneratePlcStatusControlFromGvlXml(
                inputFile,
                outputPlcStatusControlFile,
                propertiesFilePath: propertiesFile);
            _gvlXmlService.GenerateExtractedVariablesTextFromGvlXml(inputFile, outputVariableListFile);

            Console.WriteLine("Erfolg! PlcStatusControl und Variablenliste wurden erstellt.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Resolves the project root by searching upward from the current base directory
    /// until a csproj file is found.
    /// </summary>
    /// <returns>The detected project path or the current working directory as a fallback.</returns>
    private static string ResolveProjectRoot()
    {
        string current = AppContext.BaseDirectory;

        for (int i = 0; i < 8; i++)
        {
            if (Directory.EnumerateFiles(current, "*.csproj", SearchOption.TopDirectoryOnly).Any())
                return current;

            var parent = Directory.GetParent(current);
            if (parent == null)
                break;

            current = parent.FullName;
        }

        return Directory.GetCurrentDirectory();
    }
}
