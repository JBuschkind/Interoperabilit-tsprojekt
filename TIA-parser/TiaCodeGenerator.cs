using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TiaPortalParser
{
    /// <summary>
    /// Configuration for the C# class file that the generator produces.
    /// </summary>
    public class CodeGeneratorConfig
    {
        /// <summary>Namespace written at the top of the file, e.g. "OPT.Framework.MyProject.Hardware".</summary>
        public string Namespace { get; set; } = "MyProject.Hardware";

        /// <summary>Class name, e.g. "Sps".</summary>
        public string ClassName { get; set; } = "Sps";

        /// <summary>
        /// Optional extra using-directives (one per entry, without the 'using' keyword or semicolon).
        /// The ReSharper suppression comment and the class summary are always emitted.
        /// </summary>
        public List<string> AdditionalUsings { get; set; } = new();

        /// <summary>
        /// When true, each property is emitted as 'public virtual T Name'.
        /// When false, 'public T Name' is used instead.
        /// </summary>
        public bool UseVirtualProperties { get; set; } = true;
    }

    /// <summary>
    /// Generates a C# class file from a list of <see cref="TiaVariable"/> objects.
    /// </summary>
    public static class TiaCodeGenerator
    {
        /// <summary>
        /// Generates a C# source file and writes it to <paramref name="outputPath"/>.
        /// </summary>
        public static void GenerateFile(
            List<TiaVariable> variables,
            CodeGeneratorConfig config,
            string outputPath)
        {
            string content = Generate(variables, config);
            File.WriteAllText(outputPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        /// <summary>
        /// Generates and returns the C# source as a string.
        /// </summary>
        public static string Generate(
            List<TiaVariable> variables,
            CodeGeneratorConfig config)
        {
            var sb = new StringBuilder();

            AppendHeader(sb, config);
            AppendNamespaceOpen(sb, config);
            AppendClassOpen(sb, config);
            AppendConstructor(sb, config);
            AppendProperties(sb, variables, config);
            AppendClassClose(sb);
            AppendNamespaceClose(sb);

            return sb.ToString();
        }

        private static void AppendHeader(StringBuilder sb, CodeGeneratorConfig config)
        {
            // Additional using directives
            foreach (string u in config.AdditionalUsings)
                sb.AppendLine($"using {u};");

            if (config.AdditionalUsings.Count > 0)
                sb.AppendLine();

            // ReSharper suppression + alternative comment
            sb.AppendLine("// ReSharper disable UnusedAutoPropertyAccessor.Global");
            sb.AppendLine();
            sb.AppendLine();
        }

        private static void AppendNamespaceOpen(StringBuilder sb, CodeGeneratorConfig config)
        {
            sb.AppendLine($"namespace {config.Namespace}");
            sb.AppendLine("{");
        }

        private static void AppendClassOpen(StringBuilder sb, CodeGeneratorConfig config)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Variables definition");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public class {config.ClassName}");
            sb.AppendLine("    {");
        }

        private static void AppendConstructor(StringBuilder sb, CodeGeneratorConfig config)
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Ctor");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public {config.ClassName}()");
            sb.AppendLine("        {");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void AppendProperties(
            StringBuilder sb,
            List<TiaVariable> vars,
            CodeGeneratorConfig config)
        {
            for (int i = 0; i < vars.Count; i++)
            {
                TiaVariable v = vars[i];

                string csType = TiaCodeHelper.MapType(v.DataType);
                string propName = TiaCodeHelper.ToPascalCase(v.Name);
                string modifier = config.UseVirtualProperties ? "virtual " : string.Empty;

                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// {TiaCodeHelper.EscapeXmlComment(v.Comment)}");
                    sb.AppendLine("        /// </summary>");

                sb.AppendLine($"        public {modifier}{csType} {propName} {{ get; set; }}");

                // Blank line between properties, not after the last one
                if (i < vars.Count - 1)
                    sb.AppendLine();
            }
        }

        private static void AppendClassClose(StringBuilder sb)
        {
            sb.AppendLine("    }");
        }

        private static void AppendNamespaceClose(StringBuilder sb)
        {
            sb.AppendLine("}");
        }
    }
}
