using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunicateUtil.Generator.Models;

namespace CommunicateUtil.Generator
{
    public sealed class CommunicationCodeGenerator : ICommunicationCodeGenerator
    {
        private readonly ICommunicationSchemaValidator _validator;

        public CommunicationCodeGenerator()
            : this(new CommunicationSchemaValidator())
        {
        }

        public CommunicationCodeGenerator(ICommunicationSchemaValidator validator)
        {
            _validator = validator;
        }

        public Task<GenerationResult> GenerateAsync(GenerateRequest request)
        {
            var result = new GenerationResult();
            var validation = _validator.Validate(request.Schema);
            if (!validation.IsValid)
            {
                result.Diagnostics.AddRange(validation.Errors);
                return Task.FromResult(result);
            }

            result.Files.AddRange(BuildFiles(request.Schema));
            result.ZipBytes = BuildZip(result.Files);
            if (request.OutputMode == GenerateOutputMode.WriteLocal)
            {
                result.LocalOutputDirectory = WriteFiles(request.OutputRootDirectory, request.Schema.ProjectName, result.Files);
            }

            result.Success = true;
            return Task.FromResult(result);
        }

        public Task<GenerationResult> GenerateClassAsync(ProtocolSchema schema, string className)
        {
            var result = new GenerationResult();
            if (schema == null)
            {
                result.Diagnostics.Add("配置不能为空。");
                return Task.FromResult(result);
            }

            var cls = schema.Classes.FirstOrDefault(c => string.Equals(c.Name, className, StringComparison.Ordinal));
            if (cls == null)
            {
                result.Diagnostics.Add("未找到通讯类：" + className);
                return Task.FromResult(result);
            }

            result.Files.Add(new GeneratedFile("Models/" + cls.Name + ".cs", BuildClassFile(schema, cls)));
            result.ZipBytes = BuildZip(result.Files);
            result.Success = true;
            return Task.FromResult(result);
        }

        public Task<GenerationResult> GenerateEnumAsync(ProtocolSchema schema, string enumName)
        {
            var result = new GenerationResult();
            if (schema == null)
            {
                result.Diagnostics.Add("配置不能为空。");
                return Task.FromResult(result);
            }

            var enumSchema = schema.Enums.FirstOrDefault(e => string.Equals(e.Name, enumName, StringComparison.Ordinal));
            if (enumSchema == null)
            {
                result.Diagnostics.Add("未找到枚举：" + enumName);
                return Task.FromResult(result);
            }

            result.Files.Add(new GeneratedFile("Enums/" + enumSchema.Name + ".cs", BuildEnumFile(schema, enumSchema)));
            result.ZipBytes = BuildZip(result.Files);
            result.Success = true;
            return Task.FromResult(result);
        }

        private static IEnumerable<GeneratedFile> BuildFiles(ProtocolSchema schema)
        {
            var files = new List<GeneratedFile>();
            files.Add(new GeneratedFile(schema.ProjectName + ".csproj", BuildProjectFile(schema)));
            files.Add(new GeneratedFile("README.md", BuildReadme(schema)));
            files.Add(new GeneratedFile("configuration.json", BuildConfigurationJson(schema)));

            foreach (var enumSchema in schema.Enums.OrderBy(e => e.Name))
            {
                files.Add(new GeneratedFile("Enums/" + enumSchema.Name + ".cs", BuildEnumFile(schema, enumSchema)));
            }

            foreach (var cls in schema.Classes.OrderBy(c => c.Name))
            {
                files.Add(new GeneratedFile("Models/" + cls.Name + ".cs", BuildClassFile(schema, cls)));
            }

            if (schema.ValidationMethods.Count > 0)
            {
                files.Add(new GeneratedFile("Validation/ProtocolValidationMethods.cs", BuildValidationFile(schema)));
            }

            files.Add(new GeneratedFile("Examples/" + schema.ProjectName + "Example.cs", BuildExampleFile(schema)));
            return files;
        }

        private static string BuildProjectFile(ProtocolSchema schema)
        {
            return @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <RootNamespace>" + schema.Namespace + @"</RootNamespace>
    <AssemblyName>" + schema.AssemblyName + @"</AssemblyName>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""..\..\..\..\CommunicateUtil\CommunicateUtil.csproj"" Condition=""Exists('..\..\..\..\CommunicateUtil\CommunicateUtil.csproj')"" />
  </ItemGroup>

</Project>
";
        }

        private static string BuildEnumFile(ProtocolSchema schema, EnumSchema enumSchema)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace " + schema.Namespace);
            sb.AppendLine("{");
            if (!string.IsNullOrWhiteSpace(enumSchema.Desc))
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// " + EscapeXml(enumSchema.Desc));
                sb.AppendLine("    /// </summary>");
            }
            sb.AppendLine("    public enum " + enumSchema.Name + " : " + enumSchema.UnderlyingType);
            sb.AppendLine("    {");
            for (var i = 0; i < enumSchema.Members.Count; i++)
            {
                var member = enumSchema.Members[i];
                if (!string.IsNullOrWhiteSpace(member.Desc))
                {
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine("        /// " + EscapeXml(member.Desc));
                    sb.AppendLine("        /// </summary>");
                }
                sb.Append("        " + member.Name + " = " + member.Value.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine(i == enumSchema.Members.Count - 1 ? string.Empty : ",");
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildClassFile(ProtocolSchema schema, CommClassSchema cls)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using CommunicateUtil;");
            if (cls.Fields.Any(f => f.CollectionKind == FieldCollectionKind.List))
            {
                sb.AppendLine("using System.Collections.Generic;");
            }
            sb.AppendLine();
            sb.AppendLine("namespace " + schema.Namespace);
            sb.AppendLine("{");
            if (!string.IsNullOrWhiteSpace(cls.Desc))
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// " + EscapeXml(cls.Desc));
                sb.AppendLine("    /// </summary>");
            }
            sb.AppendLine("    public class " + cls.Name + " : BaseCommunicateArrtObject");
            sb.AppendLine("    {");
            foreach (var field in cls.Fields.OrderBy(f => ParseOrderIndex(f.OrderIndex)))
            {
                if (!string.IsNullOrWhiteSpace(field.ValidationMethodName))
                {
                    sb.AppendLine("        [ValidCheckArrtibute(\"" + EscapeString(schema.AssemblyName) + "\", \"" + EscapeString(field.ValidationMethodName) + "\")]");
                }
                sb.AppendLine("        " + BuildCommunicateAttribute(field));
                sb.AppendLine("        public " + BuildPropertyType(field) + " " + field.Name + " { get; set; }");
                sb.AppendLine();
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildCommunicateAttribute(CommFieldSchema field)
        {
            var args = new List<string>();
            args.Add("OrderIndex = " + FormatOrderIndex(field.OrderIndex));
            if (field.StartIndex != -1)
            {
                args.Add("StartIndex = " + field.StartIndex.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrWhiteSpace(field.ArrayLength) && field.ArrayLength != "-1")
            {
                args.Add("ArrayLength = \"" + EscapeString(field.ArrayLength) + "\"");
            }
            if (!string.IsNullOrWhiteSpace(field.AutoLengthType))
            {
                args.Add("AutoLengthType = typeof(" + field.AutoLengthType + ")");
            }
            if (!string.IsNullOrWhiteSpace(field.EnumEndType))
            {
                args.Add("EnumEndType = typeof(" + field.EnumEndType + ")");
            }
            if (!string.IsNullOrWhiteSpace(field.EndianType) && field.EndianType != "Big_ABCD")
            {
                args.Add("EndianType = EndianType." + field.EndianType);
            }
            if (!string.IsNullOrWhiteSpace(field.Desc))
            {
                args.Add("Desc = \"" + EscapeString(field.Desc) + "\"");
            }
            if (!string.IsNullOrWhiteSpace(field.Remarks))
            {
                args.Add("Remarks = \"" + EscapeString(field.Remarks) + "\"");
            }
            return "[CommunicateArrtibute(" + string.Join(", ", args) + ")]";
        }

        private static string BuildPropertyType(CommFieldSchema field)
        {
            var typeName = field.TypeKind == FieldTypeKind.String ? "string" : field.TypeName;
            if (field.CollectionKind == FieldCollectionKind.Array)
            {
                return typeName + "[]";
            }
            if (field.CollectionKind == FieldCollectionKind.List)
            {
                return "List<" + typeName + ">";
            }
            return typeName;
        }

        private static string BuildValidationFile(ProtocolSchema schema)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using CommunicateUtil;");
            sb.AppendLine();
            sb.AppendLine("namespace " + schema.Namespace);
            sb.AppendLine("{");
            sb.AppendLine("    public static class ProtocolValidationMethods");
            sb.AppendLine("    {");
            foreach (var method in schema.ValidationMethods.OrderBy(m => m.Name))
            {
                sb.AppendLine("        [ValidationMethod]");
                sb.AppendLine("        public static bool " + method.Name + "(object target)");
                sb.AppendLine("        {");
                foreach (var line in (string.IsNullOrWhiteSpace(method.Body) ? "return true;" : method.Body).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    sb.AppendLine("            " + line);
                }
                sb.AppendLine("        }");
                sb.AppendLine();
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildExampleFile(ProtocolSchema schema)
        {
            var firstClass = schema.Classes.OrderBy(c => c.Name).First();
            var sb = new StringBuilder();
            sb.AppendLine("using System.Linq;");
            sb.AppendLine();
            sb.AppendLine("namespace " + schema.Namespace + ".Examples");
            sb.AppendLine("{");
            sb.AppendLine("    public static class " + schema.ProjectName + "Example");
            sb.AppendLine("    {");
            sb.AppendLine("        public static byte[] EncodeDefault()");
            sb.AppendLine("        {");
            sb.AppendLine("            var model = new " + schema.Namespace + "." + firstClass.Name + "();");
            sb.AppendLine("            return model.GetBytes();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static " + schema.Namespace + "." + firstClass.Name + " Decode(byte[] bytes)");
            sb.AppendLine("        {");
            sb.AppendLine("            return " + schema.Namespace + "." + firstClass.Name + ".GetSelf<" + schema.Namespace + "." + firstClass.Name + ">(bytes);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildReadme(ProtocolSchema schema)
        {
            return "# " + schema.ProjectName + Environment.NewLine + Environment.NewLine +
                "Generated CommunicateUtil model library." + Environment.NewLine + Environment.NewLine +
                "- Target framework: netstandard2.0" + Environment.NewLine +
                "- Depends on: CommunicateUtil" + Environment.NewLine +
                "- Generated classes inherit BaseCommunicateArrtObject." + Environment.NewLine;
        }

        private static string BuildConfigurationJson(ProtocolSchema schema)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"projectName\": \"" + EscapeString(schema.ProjectName) + "\",");
            sb.AppendLine("  \"namespace\": \"" + EscapeString(schema.Namespace) + "\",");
            sb.AppendLine("  \"assemblyName\": \"" + EscapeString(schema.AssemblyName) + "\",");
            sb.AppendLine("  \"targetFramework\": \"netstandard2.0\",");
            sb.AppendLine("  \"classes\": [");
            for (var i = 0; i < schema.Classes.Count; i++)
            {
                var cls = schema.Classes[i];
                sb.AppendLine("    { \"name\": \"" + EscapeString(cls.Name) + "\", \"fieldCount\": " + cls.Fields.Count.ToString(CultureInfo.InvariantCulture) + " }" + (i == schema.Classes.Count - 1 ? string.Empty : ","));
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static byte[] BuildZip(IEnumerable<GeneratedFile> files)
        {
            using (var stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in files)
                    {
                        var entry = archive.CreateEntry(file.Path.Replace('\\', '/'));
                        using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                        {
                            writer.Write(file.Content);
                        }
                    }
                }
                return stream.ToArray();
            }
        }

        private static string WriteFiles(string outputRootDirectory, string projectName, IEnumerable<GeneratedFile> files)
        {
            if (string.IsNullOrWhiteSpace(outputRootDirectory))
            {
                throw new InvalidOperationException("本地输出根目录不能为空。");
            }

            var root = Path.GetFullPath(outputRootDirectory);
            var output = Path.GetFullPath(Path.Combine(root, projectName));
            if (!output.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("本地输出目录超出允许范围。");
            }

            Directory.CreateDirectory(output);
            foreach (var file in files)
            {
                var path = Path.GetFullPath(Path.Combine(output, file.Path));
                if (!path.StartsWith(output, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("生成文件路径超出允许范围：" + file.Path);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, file.Content, new UTF8Encoding(false));
            }

            return output;
        }

        private static float ParseOrderIndex(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        private static string FormatOrderIndex(string value)
        {
            var trimmed = value.Trim();
            return trimmed.IndexOf('.') >= 0 ? trimmed + "f" : trimmed;
        }

        private static string EscapeString(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string EscapeXml(string value)
        {
            return (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
