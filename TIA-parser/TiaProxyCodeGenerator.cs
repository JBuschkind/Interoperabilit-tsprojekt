using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TiaPortalParser
{
    /// <summary>
    /// Generates a C# proxy class that reads/writes values from/to an OPC UA server using <see cref="IOpcValueReader"/> and <see cref="IOpcValueWriter"/>.
    /// </summary>
    public static class TiaProxyGenerator
    {
        /// <summary>
        /// Generates a C# source file and writes it to <paramref name="outputPath"/>.
        /// </summary>
        public static void GenerateFile(
            TiaDataBlock dataBlock,
            CodeGeneratorConfig config,
            string outputPath)
        {
            string content = Generate(dataBlock, config);
            File.WriteAllText(outputPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        /// <summary>
        /// Generates and returns the C# source as a string.
        /// </summary>
        public static string Generate(
            TiaDataBlock dataBlock,
            CodeGeneratorConfig config)
        {
            var vars = dataBlock.Variables.ToList();

            var readable = vars.Where(v => v.ExternalWritable != true).ToList();
            var writable = vars.Where(v => v.ExternalWritable == true).ToList();

            var sb = new StringBuilder();

            AppendHeader(sb, config);
            AppendNamespaceOpen(sb, config);
            AppendClass(sb, dataBlock, readable, writable, config);
            AppendNamespaceClose(sb);

            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────

        private static void AppendHeader(StringBuilder sb, CodeGeneratorConfig config)
        {
            sb.AppendLine("using Opc.Ua;");
            sb.AppendLine("using System;");
            
            foreach (var u in config.AdditionalUsings)
                sb.AppendLine($"using {u};");

            sb.AppendLine();
            sb.AppendLine("// ReSharper disable StringLiteralTypo");
            sb.AppendLine();
        }

        private static void AppendNamespaceOpen(StringBuilder sb, CodeGeneratorConfig config)
        {
            sb.AppendLine($"namespace {config.Namespace}");
            sb.AppendLine("{");
        }

        private static void AppendNamespaceClose(StringBuilder sb)
        {
            sb.AppendLine("}");
        }

        // ─────────────────────────────────────────────────────────────

        private static void AppendClass(
            StringBuilder sb,
            TiaDataBlock dataBlock,
            List<TiaVariable> readable,
            List<TiaVariable> writable,
            CodeGeneratorConfig config)
        {
            string className = config.ClassName + "Proxy";

            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine($"    public class {className} : {config.ClassName}");
            sb.AppendLine("    {");

            AppendFields(sb, dataBlock.Variables, dataBlock.Name);
            AppendCtor(sb, className, config.ClassName);
            AppendProperties(sb, dataBlock.Variables);
            AppendReadValues(sb, readable);
            AppendIsUpdateRequired(sb);

            sb.AppendLine("    }");
        }

        // ─────────────────────────────────────────────────────────────

        private static void AppendFields(StringBuilder sb, List<TiaVariable> vars, string dbName)
        {
            sb.AppendLine("        private readonly IOpcValueReader _opcValueReader;");
            sb.AppendLine("        private readonly IOpcValueWriter _opcValueWriter;");
            sb.AppendLine($"        private readonly {nameof(TiaVariable).Replace(nameof(TiaVariable), "Sps")} _model;");
            sb.AppendLine("        private DateTime _lastRead;");
            sb.AppendLine("        private readonly TimeSpan _updateInterval;");
            sb.AppendLine();

            foreach (var v in vars)
            {
                string propName = TiaCodeHelper.ToPascalCase(v.Name);
                string fieldName = GetNodeFieldName(propName);

                sb.AppendLine($"        private readonly NodeId {fieldName} =");
                sb.AppendLine($"            NodeIdFactory.Create(\"{dbName}\", \"{v.StructPath}\", \"{v.Name}\", 3);");
                sb.AppendLine();
            }
        }

        // ─────────────────────────────────────────────────────────────

        private static void AppendCtor(StringBuilder sb, string proxyClassName, string baseClassName)
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Ctor");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public {proxyClassName}(IOpcValueReader opcValueReader, IOpcValueWriter opcValueWriter)");
            sb.AppendLine("        {");
            sb.AppendLine("            _opcValueReader = opcValueReader;");
            sb.AppendLine("            _opcValueWriter = opcValueWriter;");
            sb.AppendLine($"            _model = new {baseClassName}();");
            sb.AppendLine("            _lastRead = DateTime.MinValue;");
            sb.AppendLine("            _updateInterval = TimeSpan.FromMilliseconds(500);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // ─────────────────────────────────────────────────────────────

        private static void AppendProperties(StringBuilder sb, List<TiaVariable> vars)
        {
            foreach (var v in vars)
            {
                string csType = TiaCodeHelper.MapType(v.DataType);
                string propName = TiaCodeHelper.ToPascalCase(v.Name);
                string fieldName = GetNodeFieldName(propName);

                sb.AppendLine("        /// <inheritdoc />");
                sb.AppendLine($"        public override {csType} {propName}");
                sb.AppendLine("        {");

                if (v.ExternalWritable == true)
                {
                    // setter only
                    sb.AppendLine("            set");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                _opcValueWriter.Write({fieldName}, value);");
                    sb.AppendLine("            }");
                }
                else
                {
                    // getter only
                    sb.AppendLine("            get");
                    sb.AppendLine("            {");
                    sb.AppendLine("                ReadValues();");
                    sb.AppendLine($"                return _model.{propName};");
                    sb.AppendLine("            }");
                }

                sb.AppendLine("        }");
                sb.AppendLine();
            }
        }

        // ─────────────────────────────────────────────────────────────

        private static void AppendReadValues(StringBuilder sb, List<TiaVariable> readable)
        {
            sb.AppendLine("        private void ReadValues()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (!IsUpdateRequired()) return;");
            sb.AppendLine();

            foreach (var v in readable)
            {
                string csType = TiaCodeHelper.MapType(v.DataType);
                string propName = TiaCodeHelper.ToPascalCase(v.Name);
                string fieldName = GetNodeFieldName(propName);

                sb.AppendLine($"            _model.{propName} = _opcValueReader.ReadValue<{csType}>({fieldName});");
            }

            sb.AppendLine();
            sb.AppendLine("            _lastRead = DateTime.Now;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void AppendIsUpdateRequired(StringBuilder sb)
        {
            sb.AppendLine("        private bool IsUpdateRequired()");
            sb.AppendLine("        {");
            sb.AppendLine("            return DateTime.Now - _lastRead > _updateInterval;");
            sb.AppendLine("        }");
        }

        // ─────────────────────────────────────────────────────────────

        private static string GetNodeFieldName(string propName)
        {
            return "_" + char.ToLowerInvariant(propName[0]) + propName.Substring(1) + "NodeId";
        }
    }
}