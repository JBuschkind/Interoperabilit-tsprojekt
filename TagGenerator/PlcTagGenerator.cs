using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Draeger.PlcGeneration
{
    /// <summary>
    /// Repräsentiert eine PLC-Variable inklusive aller aus TIA-Export auslesbaren Metadaten.
    /// </summary>
    public sealed class PlcTag<T>
    {
        public string Name { get; }
        public string DataType { get; }
        public string IoType { get; }
        public string LogicalAddress { get; }
        public string Comment { get; }

        // Hardware-Zuordnung (optional, falls verlinkt)
        public string ModuleName { get; }
        public string ChannelName { get; }
        public int? ChannelNumber { get; }
        public string ChannelIoType { get; }

        public PlcTag(
            string name,
            string dataType,
            string ioType,
            string logicalAddress,
            string comment,
            string moduleName,
            string channelName,
            int? channelNumber,
            string channelIoType)
        {
            Name = name;
            DataType = dataType;
            IoType = ioType;
            LogicalAddress = logicalAddress;
            Comment = comment;
            ModuleName = moduleName;
            ChannelName = channelName;
            ChannelNumber = channelNumber;
            ChannelIoType = channelIoType;
        }
    }

    /// <summary>
    /// Liest einen TIA-AML-Export und generiert C#-Code mit Getter-Eigenschaften für alle Tags.
    /// </summary>
    public static class PlcTagGenerator
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Verwendung: PlcTagGenerator <Pfad zu TIA_Export.aml> <Ausgabedatei.cs>");
                return;
            }

            var amlPath = args[0];
            var outputPath = args[1];

            if (!File.Exists(amlPath))
            {
                Console.WriteLine($"AML-Datei nicht gefunden: {amlPath}");
                return;
            }

            var doc = XDocument.Load(amlPath);

            var channelMap = BuildChannelMap(doc);

            var tagMap = BuildTagMap(doc);

            LinkTagsToChannels(doc, tagMap, channelMap);

            var code = GenerateCode(tagMap.Values.OrderBy(t => t.Name).ToList());

            File.WriteAllText(outputPath, code, Encoding.UTF8);
            Console.WriteLine($"C#-Code generiert: {outputPath}");
        }

        private sealed class ChannelInfo
        {
            public string ModuleName { get; init; }
            public string ChannelName { get; init; }
            public int? ChannelNumber { get; init; }
            public string IoType { get; init; }
            public string Type { get; init; }
        }

        private sealed class TagInfo
        {
            public string Key { get; init; }
            public string Name { get; init; }
            public string DataType { get; init; }
            public string IoType { get; init; }
            public string LogicalAddress { get; init; }
            public string Comment { get; init; }

            // Wird über InternalLink gefüllt
            public ChannelInfo Channel { get; set; }
        }

        private static Dictionary<string, ChannelInfo> BuildChannelMap(XDocument doc)
        {
            var ns = doc.Root;
            var result = new Dictionary<string, ChannelInfo>();

            var internalElements = doc.Descendants("InternalElement");

            foreach (var ie in internalElements)
            {
                var internalId = (string)ie.Attribute("ID");
                if (string.IsNullOrEmpty(internalId))
                    continue;

                // Modulname aus übergeordnetem InternalElement oder TypeName
                var moduleElement = ie.Ancestors("InternalElement").FirstOrDefault()
                                   ?? ie;

                var moduleName = (string)moduleElement.Attribute("Name")
                                ?? moduleElement
                                    .Elements("Attribute")
                                    .FirstOrDefault(a => (string)a.Attribute("Name") == "TypeName")
                                    ?.Element("Value")?.Value;

                foreach (var ext in ie.Elements("ExternalInterface")
                                      .Where(e => (string)e.Attribute("RefBaseClassPath") ==
                                                  "AutomationProjectConfigurationInterfaceClassLib/Channel"))
                {
                    var ifaceName = (string)ext.Attribute("Name");
                    if (string.IsNullOrEmpty(ifaceName))
                        continue;

                    var key = $"{internalId}:{ifaceName}";

                    int? number = null;
                    var numberAttr = GetAttributeValue(ext, "Number");
                    if (int.TryParse(numberAttr, out var parsed))
                        number = parsed;

                    var channelInfo = new ChannelInfo
                    {
                        ModuleName = moduleName,
                        ChannelName = ifaceName,
                        ChannelNumber = number,
                        IoType = GetAttributeValue(ext, "IoType"),
                        Type = GetAttributeValue(ext, "Type")
                    };

                    result[key] = channelInfo;
                }
            }

            return result;
        }

        private static Dictionary<string, TagInfo> BuildTagMap(XDocument doc)
        {
            var result = new Dictionary<string, TagInfo>();

            foreach (var parent in doc.Descendants("InternalElement"))
            {
                var parentId = (string)parent.Attribute("ID");
                if (string.IsNullOrEmpty(parentId))
                    continue;

                foreach (var ext in parent.Elements("ExternalInterface")
                                          .Where(e => (string)e.Attribute("RefBaseClassPath") ==
                                                      "AutomationProjectConfigurationInterfaceClassLib/Tag"))
                {
                    var tagName = (string)ext.Attribute("Name");
                    if (string.IsNullOrEmpty(tagName))
                        continue;

                    var key = $"{parentId}:{tagName}";

                    var info = new TagInfo
                    {
                        Key = key,
                        Name = tagName,
                        DataType = GetAttributeValue(ext, "DataType"),
                        IoType = GetAttributeValue(ext, "IoType"),
                        LogicalAddress = GetAttributeValue(ext, "LogicalAddress"),
                        Comment = GetAttributeValue(ext, "Comment")
                    };

                    result[key] = info;
                }
            }

            return result;
        }

        private static void LinkTagsToChannels(
            XDocument doc,
            Dictionary<string, TagInfo> tagMap,
            Dictionary<string, ChannelInfo> channelMap)
        {
            foreach (var link in doc.Descendants("InternalLink"))
            {
                var a = (string)link.Attribute("RefPartnerSideA");
                var b = (string)link.Attribute("RefPartnerSideB");
                if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                    continue;

                if (channelMap.TryGetValue(a, out var channel) && tagMap.TryGetValue(b, out var tag))
                {
                    tag.Channel = channel;
                }
                else if (channelMap.TryGetValue(b, out channel) && tagMap.TryGetValue(a, out var tag2))
                {
                    tag2.Channel = channel;
                }
            }
        }

        private static string GetAttributeValue(XElement parent, string attributeName)
        {
            var attr = parent.Elements("Attribute")
                             .FirstOrDefault(a => (string)a.Attribute("Name") == attributeName);
            return attr?.Element("Value")?.Value ?? string.Empty;
        }

        private static string GenerateCode(IReadOnlyCollection<TagInfo> tags)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("namespace Draeger.Plc");
            sb.AppendLine("{");
            sb.AppendLine("    // Automatisch generiert aus TIA_Export.aml");
            sb.AppendLine("    public static class PlcTags");
            sb.AppendLine("    {");

            foreach (var tag in tags)
            {
                var clrType = MapDataTypeToClrType(tag.DataType);
                var safeName = MakeSafeIdentifier(tag.Name);

                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Name: {tag.Name}");
                if (!string.IsNullOrEmpty(tag.Comment))
                    sb.AppendLine($"        /// Kommentar: {EscapeForXmlComment(tag.Comment)}");
                if (!string.IsNullOrEmpty(tag.DataType))
                    sb.AppendLine($"        /// Datentyp: {tag.DataType}");
                if (!string.IsNullOrEmpty(tag.IoType))
                    sb.AppendLine($"        /// IO-Typ: {tag.IoType}");
                if (!string.IsNullOrEmpty(tag.LogicalAddress))
                    sb.AppendLine($"        /// Adresse: {tag.LogicalAddress}");
                if (tag.Channel != null)
                {
                    if (!string.IsNullOrEmpty(tag.Channel.ModuleName))
                        sb.AppendLine($"        /// Modul: {tag.Channel.ModuleName}");
                    if (!string.IsNullOrEmpty(tag.Channel.ChannelName))
                        sb.AppendLine($"        /// Kanal: {tag.Channel.ChannelName} ({tag.Channel.ChannelNumber})");
                    if (!string.IsNullOrEmpty(tag.Channel.IoType))
                        sb.AppendLine($"        /// Kanal-IO-Typ: {tag.Channel.IoType}");
                    if (!string.IsNullOrEmpty(tag.Channel.Type))
                        sb.AppendLine($"        /// Kanal-Typ: {tag.Channel.Type}");
                }
                sb.AppendLine("        /// </summary>");

                sb.AppendLine(
                    $"        public static PlcTag<{clrType}> {safeName} => new PlcTag<{clrType}>(");
                sb.AppendLine($"            name: \"{tag.Name}\",");
                sb.AppendLine($"            dataType: \"{tag.DataType}\",");
                sb.AppendLine($"            ioType: \"{tag.IoType}\",");
                sb.AppendLine($"            logicalAddress: \"{tag.LogicalAddress}\",");
                sb.AppendLine($"            comment: \"{EscapeForString(tag.Comment)}\",");
                sb.AppendLine($"            moduleName: \"{EscapeForString(tag.Channel?.ModuleName ?? string.Empty)}\",");
                sb.AppendLine($"            channelName: \"{EscapeForString(tag.Channel?.ChannelName ?? string.Empty)}\",");

                var channelNumberLiteral = tag.Channel?.ChannelNumber.HasValue == true
                    ? tag.Channel.ChannelNumber.Value.ToString()
                    : "null";

                sb.AppendLine($"            channelNumber: {channelNumberLiteral},");
                sb.AppendLine(
                    $"            channelIoType: \"{EscapeForString(tag.Channel?.IoType ?? string.Empty)}\");");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string MapDataTypeToClrType(string dataType)
        {
            switch (dataType?.Trim())
            {
                case "Bool":
                case "BOOL":
                    return "bool";
                case "Int":
                case "INT":
                    return "short";
                case "DInt":
                case "DINT":
                    return "int";
                case "Real":
                case "REAL":
                    return "float";
                case "LReal":
                case "LREAL":
                    return "double";
                case "Byte":
                case "BYTE":
                    return "byte";
                case "Word":
                case "WORD":
                    return "ushort";
                case "DWord":
                case "DWORD":
                    return "uint";
                default:
                    return "object";
            }
        }

        private static string MakeSafeIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "_unnamed";

            var sb = new StringBuilder();

            var first = name[0];
            if (char.IsLetter(first) || first == '_')
                sb.Append(first);
            else
                sb.Append('_');

            for (var i = 1; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }

            return sb.ToString();
        }

        private static string EscapeForString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static string EscapeForXmlComment(string value)
        {
            return (value ?? string.Empty).Replace("--", "- -");
        }
    }
}

