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
    /// <summary>
    /// 根据通讯协议配置生成可编译的通讯模型类库源码。
    /// </summary>
    public sealed class CommunicationCodeGenerator : ICommunicationCodeGenerator
    {
        private readonly ICommunicationSchemaValidator _validator;

        /// <summary>
        /// 使用默认协议配置校验器初始化代码生成器。
        /// </summary>
        public CommunicationCodeGenerator()
            : this(new CommunicationSchemaValidator())
        {
        }

        /// <summary>
        /// 使用指定协议配置校验器初始化代码生成器。
        /// </summary>
        /// <param name="validator">协议配置校验器。</param>
        public CommunicationCodeGenerator(ICommunicationSchemaValidator validator)
        {
            _validator = validator;
        }

        /// <summary>
        /// 根据完整生成请求生成项目、模型、枚举、配置、示例和 ZIP 包。
        /// </summary>
        /// <param name="request">生成请求。</param>
        /// <returns>生成结果。</returns>
        public Task<GenerationResult> GenerateAsync(GenerateRequest request)
        {
            var result = new GenerationResult();
            var validation = _validator.Validate(request.Schema);
            if (!validation.IsValid)
            {
                // 配置校验失败时直接返回诊断信息，避免生成不可编译或不安全的代码。
                result.Diagnostics.AddRange(validation.Errors);
                return Task.FromResult(result);
            }

            // 先构建内存中的文件集合和 ZIP，确保预览、下载和本地写入使用同一份生成结果。
            result.Files.AddRange(BuildFiles(request.Schema));
            result.ZipBytes = BuildZip(result.Files);
            if (request.OutputMode == GenerateOutputMode.WriteLocal)
            {
                // 仅在用户明确选择本地输出时写入磁盘，普通预览和下载模式不会改动本地文件。
                result.LocalOutputDirectory = WriteFiles(request.OutputRootDirectory, request.Schema.ProjectName, result.Files);
            }

            result.Success = true;
            return Task.FromResult(result);
        }

        /// <summary>
        /// 生成指定通讯类的单个模型文件。
        /// </summary>
        /// <param name="schema">协议配置。</param>
        /// <param name="className">通讯类名称。</param>
        /// <returns>包含指定通讯类文件的生成结果。</returns>
        public Task<GenerationResult> GenerateClassAsync(ProtocolSchema schema, string className)
        {
            var result = new GenerationResult();
            if (schema == null)
            {
                result.Diagnostics.Add("配置不能为空。");
                return Task.FromResult(result);
            }

            // 使用精确大小写匹配，避免生成出与配置名称不一致的文件路径和类型名。
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

        /// <summary>
        /// 生成指定枚举的单个枚举文件。
        /// </summary>
        /// <param name="schema">协议配置。</param>
        /// <param name="enumName">枚举名称。</param>
        /// <returns>包含指定枚举文件的生成结果。</returns>
        public Task<GenerationResult> GenerateEnumAsync(ProtocolSchema schema, string enumName)
        {
            var result = new GenerationResult();
            if (schema == null)
            {
                result.Diagnostics.Add("配置不能为空。");
                return Task.FromResult(result);
            }

            // 枚举名称同样采用精确匹配，保证生成文件名和枚举类型名来自配置原值。
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

        /// <summary>
        /// 构建完整生成项目所需的所有文件。
        /// </summary>
        /// <param name="schema">协议配置。</param>
        /// <returns>生成文件集合。</returns>
        private static IEnumerable<GeneratedFile> BuildFiles(ProtocolSchema schema)
        {
            var files = new List<GeneratedFile>();
            files.Add(new GeneratedFile(schema.ProjectName + ".csproj", BuildProjectFile(schema)));
            files.Add(new GeneratedFile("README.md", BuildReadme(schema)));
            files.Add(new GeneratedFile("configuration.json", BuildConfigurationJson(schema)));

            // 枚举和通讯类按名称排序，保证多次生成的文件顺序稳定，便于预览和版本比较。
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

            // 示例文件依赖至少一个通讯类，调用方在进入生成流程前已经通过校验确保该条件成立。
            files.Add(new GeneratedFile("Examples/" + schema.ProjectName + "Example.cs", BuildExampleFile(schema)));
            return files;
        }

        /// <summary>
        /// 构建生成项目的 SDK 风格项目文件内容。
        /// </summary>
        /// <param name="schema">协议配置。</param>
        /// <returns>项目文件文本。</returns>
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

        /// <summary>
        /// 构建指定枚举的 C# 源码文件内容。
        /// </summary>
        /// <param name="schema">协议配置。</param>
        /// <param name="enumSchema">枚举配置。</param>
        /// <returns>枚举源码文本。</returns>
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
                // 对枚举说明做 XML 转义，避免描述文本中的特殊字符破坏生成源码。
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

        /// <summary>
        /// 构建指定通讯类的 C# 源码文件内容。
        /// </summary>
        /// <param name="schema">协议配置。</param>
        /// <param name="cls">通讯类配置。</param>
        /// <returns>通讯类源码文本。</returns>
        private static string BuildClassFile(ProtocolSchema schema, CommClassSchema cls)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using CommunicateUtil;");
            if (cls.Fields.Any(f => f.CollectionKind == FieldCollectionKind.List))
            {
                // 只有存在 List 字段时才引入集合命名空间，减少生成代码中的无用 using。
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
                // 字段按通讯协议顺序输出，确保属性顺序与序列化顺序保持一致。
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

        /// <summary>
        /// 根据字段配置构建 CommunicateUtil 字段特性。
        /// </summary>
        /// <param name="field">字段配置。</param>
        /// <returns>字段特性源码。</returns>
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
                // 默认大端配置由 CommunicateUtil 处理，只有非默认字节序才显式写入特性。
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

        /// <summary>
        /// 根据字段类型和集合类型构建 C# 属性类型名。
        /// </summary>
        /// <param name="field">字段配置。</param>
        /// <returns>属性类型名。</returns>
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

        /// <summary>
        /// 构建自定义协议校验方法源码文件。
        /// </summary>
        /// <param name="schema">协议配置。</param>
        /// <returns>校验方法源码文本。</returns>
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
                // 方法体由配置提供；空方法体回退为返回 true，保证生成源码可以直接编译。
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

        /// <summary>
        /// 构建生成项目的基础编码和解码示例源码文件。
        /// </summary>
        /// <param name="schema">协议配置。</param>
        /// <returns>示例源码文本。</returns>
        private static string BuildExampleFile(ProtocolSchema schema)
        {
            // 示例以名称排序后的第一个通讯类为入口，确保生成内容稳定且无需用户额外选择。
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

        /// <summary>
        /// 构建生成项目的 README 文本。
        /// </summary>
        /// <param name="schema">协议配置。</param>
        /// <returns>README 文本。</returns>
        private static string BuildReadme(ProtocolSchema schema)
        {
            return "# " + schema.ProjectName + Environment.NewLine + Environment.NewLine +
                "Generated CommunicateUtil model library." + Environment.NewLine + Environment.NewLine +
                "- Target framework: netstandard2.0" + Environment.NewLine +
                "- Depends on: CommunicateUtil" + Environment.NewLine +
                "- Generated classes inherit BaseCommunicateArrtObject." + Environment.NewLine;
        }

        /// <summary>
        /// 构建生成项目随附的配置摘要 JSON。
        /// </summary>
        /// <param name="schema">协议配置。</param>
        /// <returns>配置摘要 JSON 文本。</returns>
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

        /// <summary>
        /// 将生成文件集合打包为 ZIP 字节数组。
        /// </summary>
        /// <param name="files">生成文件集合。</param>
        /// <returns>ZIP 文件字节。</returns>
        private static byte[] BuildZip(IEnumerable<GeneratedFile> files)
        {
            using (var stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in files)
                    {
                        // ZIP 规范使用正斜杠作为路径分隔符，这里统一路径格式以兼容不同平台。
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

        /// <summary>
        /// 将生成文件写入指定本地输出目录。
        /// </summary>
        /// <param name="outputRootDirectory">输出根目录。</param>
        /// <param name="projectName">生成项目名称。</param>
        /// <param name="files">生成文件集合。</param>
        /// <returns>实际写入的项目目录。</returns>
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
                    // 防止配置中的相对路径跳出生成目录，避免覆盖调用方未授权的本地文件。
                    throw new InvalidOperationException("生成文件路径超出允许范围：" + file.Path);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, file.Content, new UTF8Encoding(false));
            }

            return output;
        }

        /// <summary>
        /// 将字段顺序文本解析为浮点数，供排序使用。
        /// </summary>
        /// <param name="value">字段顺序文本。</param>
        /// <returns>字段顺序数值。</returns>
        private static float ParseOrderIndex(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 将字段顺序格式化为可用于特性参数的 C# 数值字面量。
        /// </summary>
        /// <param name="value">字段顺序文本。</param>
        /// <returns>C# 数值字面量。</returns>
        private static string FormatOrderIndex(string value)
        {
            var trimmed = value.Trim();
            return trimmed.IndexOf('.') >= 0 ? trimmed + "f" : trimmed;
        }

        /// <summary>
        /// 转义可嵌入 C# 字符串字面量的文本。
        /// </summary>
        /// <param name="value">原始文本。</param>
        /// <returns>转义后的文本。</returns>
        private static string EscapeString(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>
        /// 转义可嵌入 XML 文档注释的文本。
        /// </summary>
        /// <param name="value">原始文本。</param>
        /// <returns>XML 转义后的文本。</returns>
        private static string EscapeXml(string value)
        {
            return (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
