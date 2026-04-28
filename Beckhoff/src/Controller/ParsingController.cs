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
    private readonly ICSharpToGvlXmlService _cSharpToGvlXmlService;

    /// <summary>
    /// Initializes the controller with forward and reverse translation services.
    /// </summary>
    /// <param name="gvlXmlService">Service used for XML-based code generation.</param>
    /// <param name="cSharpToGvlXmlService">Service used for C# to XML reverse translation.</param>
    public ParsingController(
        IGvlXmlService gvlXmlService,
        ICSharpToGvlXmlService cSharpToGvlXmlService)
    {
        _gvlXmlService = gvlXmlService;
        _cSharpToGvlXmlService = cSharpToGvlXmlService;
    }

    /// <summary>
    /// Reads runtime flags and routes execution to the selected mode.
    /// </summary>
    /// <param name="args">CLI arguments passed to the application.</param>
    /// <returns>0 on success; otherwise 1.</returns>
    public int Run(string[] args)
    {
        if (HasFlag(args, "--help", "-h"))
        {
            PrintHelp();
            return 0;
        }

        string projectRoot = ResolveProjectRoot();
        string direction = GetOption(args, new[] { "--direction", "--mode", "-d" }, "forward").ToLowerInvariant();

        return direction switch
        {
            "forward" => RunForward(args, projectRoot),
            "reverse" => RunReverse(args, projectRoot),
            _ => FailWithUnknownDirection(direction)
        };
    }

    /// <summary>
    /// Executes forward translation from PLCopen XML to generated C# artifacts,
    /// and creates a temporary XML holder for reverse translation.
    /// </summary>
    /// <param name="args">CLI arguments that may override default forward paths.</param>
    /// <param name="projectRoot">Resolved project root directory.</param>
    /// <returns>0 on success; otherwise 1.</returns>
    private int RunForward(string[] args, string projectRoot)
    {
        string inputXmlPath = GetOption(args, "--input-xml", Path.Combine(projectRoot, "Input", "GVL_PLC.xml"));
        string outputCsPath = GetOption(args, "--output-cs", Path.Combine(projectRoot, "Output", "PlcStatusControl.generated.cs"));
        string propertiesPath = GetOption(args, "--properties", Path.Combine(projectRoot, "Input", "plcstatus.properties"));

        Console.WriteLine("Direction:                       forward");
        Console.WriteLine($"Input XML:                       {inputXmlPath}");
        Console.WriteLine($"Output PlcStatusControl:         {outputCsPath}");
        Console.WriteLine($"Configuration (properties):      {propertiesPath}");

        if (!inputXmlPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Error: --input-xml must point to an XML file.");
            return 1;
        }

        try
        {
            _gvlXmlService.GeneratePlcStatusControlFromGvlXml(
                inputXmlPath,
                outputCsPath,
                propertiesFilePath: propertiesPath);

            Console.WriteLine("Success: Forward translation completed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Executes reverse translation from a C# file back into a GVL XML file
    /// based on a previously generated XML holder/template.
    /// </summary>
    /// <param name="args">CLI arguments that may override default reverse paths.</param>
    /// <param name="projectRoot">Resolved project root directory.</param>
    /// <returns>0 on success; otherwise 1.</returns>
    private int RunReverse(string[] args, string projectRoot)
    {
        string inputCsPath = GetOption(args, "--input-cs", Path.Combine(projectRoot, "Output", "PlcStatusControl.generated.cs"));
        string defaultTemplateXmlPath = Path.Combine(projectRoot, "Output", "GVL_PLC.template.xml");
        string templateXmlPath = GetOption(args, "--template-xml", defaultTemplateXmlPath);
        string outputXmlPath = GetOption(args, "--output-xml", Path.Combine(projectRoot, "Output", "GVL_PLC.updated.xml"));
        string fallbackTemplateXmlPath = Path.Combine(projectRoot, "Input", "GVL_PLC.xml");

        Console.WriteLine("Direction:                       reverse");
        Console.WriteLine($"Input C#:                        {inputCsPath}");
        Console.WriteLine($"Input XML holder (template):     {templateXmlPath}");
        Console.WriteLine($"Output XML:                      {outputXmlPath}");

        if (!inputCsPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Error: --input-cs must point to a C# file.");
            return 1;
        }

        if (!templateXmlPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Error: --template-xml must point to an XML file.");
            return 1;
        }

        if (!File.Exists(templateXmlPath))
        {
            bool usesDefaultTemplatePath =
                string.Equals(templateXmlPath, defaultTemplateXmlPath, StringComparison.OrdinalIgnoreCase);

            if (usesDefaultTemplatePath && File.Exists(fallbackTemplateXmlPath))
            {
                templateXmlPath = fallbackTemplateXmlPath;
                Console.WriteLine($"Info: Default template not found. Using input XML as template: {templateXmlPath}");
            }
            else
            {
                Console.WriteLine($"Error: XML template file not found: {templateXmlPath}");
                Console.WriteLine("Tip: Run forward once or pass --template-xml <path-to-xml>.");
                return 1;
            }
        }

        try
        {
            bool hasVariableNameChanges =
                _cSharpToGvlXmlService.UpdateGvlXmlFromCSharp(inputCsPath, templateXmlPath, outputXmlPath);

            if (!hasVariableNameChanges)
            {
                Console.WriteLine("Info: No GVL node-name changes detected in C#. Output XML may be identical.");
                Console.WriteLine("Hint: Reverse updates names from strings like \"GVL_PLC.SomeVariable\".");
            }

            Console.WriteLine("Success: Reverse translation completed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Prints a user-friendly error for unsupported direction names.
    /// </summary>
    /// <param name="direction">Direction string provided by the caller.</param>
    /// <returns>Always returns 1.</returns>
    private static int FailWithUnknownDirection(string direction)
    {
        Console.WriteLine($"Error: Unknown direction '{direction}'. Use --direction forward or --direction reverse.");
        return 1;
    }

    /// <summary>
    /// Prints usage information for the CLI.
    /// </summary>
    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project .\\xmlParser.csproj -- [options]");
        Console.WriteLine();
        Console.WriteLine("Direction:");
        Console.WriteLine("  --direction, -d      forward | reverse");
        Console.WriteLine("  --mode               forward | reverse (legacy alias)");
        Console.WriteLine();
        Console.WriteLine("Forward options:");
        Console.WriteLine("  --input-xml          Input XML path");
        Console.WriteLine("  --output-cs          Output C# path");
        Console.WriteLine("  --output-txt         Output TXT path");
        Console.WriteLine("  --properties         Properties file path");
        Console.WriteLine();
        Console.WriteLine("Reverse options:");
        Console.WriteLine("  --input-cs           Input C# path");
        Console.WriteLine("  --template-xml       XML template path");
        Console.WriteLine("  --output-xml         Output XML path");
        Console.WriteLine();
        Console.WriteLine("General:");
        Console.WriteLine("  --help, -h           Show this help");
    }

    /// <summary>
    /// Checks whether one of the provided flags is present.
    /// </summary>
    /// <param name="args">Raw CLI argument array.</param>
    /// <param name="optionNames">Flags to check.</param>
    /// <returns>True when at least one flag is present.</returns>
    private static bool HasFlag(string[] args, params string[] optionNames)
    {
        return args.Any(arg =>
            optionNames.Any(optionName =>
                string.Equals(arg, optionName, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Resolves a named CLI option in the format "--name value".
    /// If the option is missing, the provided default value is returned.
    /// </summary>
    /// <param name="args">Raw CLI argument array.</param>
    /// <param name="optionName">Option name to find.</param>
    /// <param name="defaultValue">Fallback value when option is not set.</param>
    /// <returns>Resolved option value.</returns>
    private static string GetOption(string[] args, string optionName, string defaultValue)
    {
        return GetOption(args, new[] { optionName }, defaultValue);
    }

    /// <summary>
    /// Resolves the first matching named CLI option in the formats
    /// "--name value" or "--name=value".
    /// If the option is missing, the provided default value is returned.
    /// </summary>
    /// <param name="args">Raw CLI argument array.</param>
    /// <param name="optionNames">Option names to try in order.</param>
    /// <param name="defaultValue">Fallback value when no option is set.</param>
    /// <returns>Resolved option value.</returns>
    private static string GetOption(string[] args, string[] optionNames, string defaultValue)
    {
        foreach (string optionName in optionNames)
        {
            int optionIndex = Array.FindIndex(
                args,
                arg => string.Equals(arg, optionName, StringComparison.OrdinalIgnoreCase));

            if (optionIndex >= 0
                && optionIndex + 1 < args.Length
                && !args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                return args[optionIndex + 1];
            }

            string optionPrefix = optionName + "=";
            string? combinedOption = args.FirstOrDefault(
                arg => arg.StartsWith(optionPrefix, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(combinedOption))
                return combinedOption[optionPrefix.Length..];
        }

        return defaultValue;
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
