using System.Collections.Generic;

namespace CommunicateUtil.Generator.Models
{
    /// <summary>
    /// 描述通讯字段的基础类型分类。
    /// </summary>
    public enum FieldTypeKind
    {
        /// <summary>
        /// C# 基础值类型。
        /// </summary>
        Basic = 0,

        /// <summary>
        /// 字符串类型。
        /// </summary>
        String = 1,

        /// <summary>
        /// 协议内定义的枚举类型。
        /// </summary>
        Enum = 2,

        /// <summary>
        /// 协议内定义的通讯类类型。
        /// </summary>
        CommClass = 3
    }

    /// <summary>
    /// 描述通讯字段是否为集合类型。
    /// </summary>
    public enum FieldCollectionKind
    {
        /// <summary>
        /// 非集合字段。
        /// </summary>
        None = 0,

        /// <summary>
        /// 数组字段。
        /// </summary>
        Array = 1,

        /// <summary>
        /// 泛型列表字段。
        /// </summary>
        List = 2
    }

    /// <summary>
    /// 描述代码生成结果的输出方式。
    /// </summary>
    public enum GenerateOutputMode
    {
        /// <summary>
        /// 只返回预览文件和 ZIP 字节，不写入本地目录。
        /// </summary>
        Preview = 0,

        /// <summary>
        /// 返回可下载的 ZIP 字节。
        /// </summary>
        DownloadZip = 1,

        /// <summary>
        /// 将生成文件写入本地输出目录。
        /// </summary>
        WriteLocal = 2
    }

    /// <summary>
    /// 表示一次通讯模型代码生成所需的完整协议配置。
    /// </summary>
    public sealed class ProtocolSchema
    {
        /// <summary>
        /// 生成项目的项目名称。
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// 生成代码使用的根命名空间。
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// 生成项目的程序集名称。
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// 生成项目的目标框架。
        /// </summary>
        public string TargetFramework { get; set; }

        /// <summary>
        /// 需要生成的通讯类配置集合。
        /// </summary>
        public List<CommClassSchema> Classes { get; set; }

        /// <summary>
        /// 需要生成的枚举配置集合。
        /// </summary>
        public List<EnumSchema> Enums { get; set; }

        /// <summary>
        /// 需要生成的自定义校验方法集合。
        /// </summary>
        public List<ValidationMethodSchema> ValidationMethods { get; set; }

        /// <summary>
        /// 初始化协议配置，并设置默认项目名称、命名空间、程序集名和目标框架。
        /// </summary>
        public ProtocolSchema()
        {
            ProjectName = "GeneratedCommunicateModels";
            Namespace = "GeneratedCommunicateModels";
            AssemblyName = "GeneratedCommunicateModels";
            TargetFramework = "netstandard2.0";
            Classes = new List<CommClassSchema>();
            Enums = new List<EnumSchema>();
            ValidationMethods = new List<ValidationMethodSchema>();
        }
    }

    /// <summary>
    /// 表示一个通讯类的生成配置。
    /// </summary>
    public sealed class CommClassSchema
    {
        /// <summary>
        /// 通讯类名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 通讯类说明。
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// 通讯类字段集合。
        /// </summary>
        public List<CommFieldSchema> Fields { get; set; }

        /// <summary>
        /// 初始化通讯类配置。
        /// </summary>
        public CommClassSchema()
        {
            Name = string.Empty;
            Desc = string.Empty;
            Fields = new List<CommFieldSchema>();
        }
    }

    /// <summary>
    /// 表示一个通讯字段的生成配置。
    /// </summary>
    public sealed class CommFieldSchema
    {
        /// <summary>
        /// 字段名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 字段类型分类。
        /// </summary>
        public FieldTypeKind TypeKind { get; set; }

        /// <summary>
        /// 字段类型名称。
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 字段集合类型。
        /// </summary>
        public FieldCollectionKind CollectionKind { get; set; }

        /// <summary>
        /// 通讯序列中的字段顺序。
        /// </summary>
        public string OrderIndex { get; set; }

        /// <summary>
        /// 固定起始字节索引，-1 表示不指定。
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// 数组或字符串长度配置，-1 表示不指定固定长度。
        /// </summary>
        public string ArrayLength { get; set; }

        /// <summary>
        /// 自动长度字段使用的数据类型。
        /// </summary>
        public string AutoLengthType { get; set; }

        /// <summary>
        /// 枚举结束标记使用的数据类型。
        /// </summary>
        public string EnumEndType { get; set; }

        /// <summary>
        /// 字节序配置。
        /// </summary>
        public string EndianType { get; set; }

        /// <summary>
        /// 字段说明。
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// 字段备注。
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// 字段绑定的自定义校验方法名称。
        /// </summary>
        public string ValidationMethodName { get; set; }

        /// <summary>
        /// 初始化通讯字段配置，并设置与 CommunicateUtil 默认行为一致的字段默认值。
        /// </summary>
        public CommFieldSchema()
        {
            Name = string.Empty;
            TypeKind = FieldTypeKind.Basic;
            TypeName = "byte";
            CollectionKind = FieldCollectionKind.None;
            OrderIndex = "0";
            StartIndex = -1;
            ArrayLength = "-1";
            AutoLengthType = string.Empty;
            EnumEndType = string.Empty;
            EndianType = "Big_ABCD";
            Desc = string.Empty;
            Remarks = string.Empty;
            ValidationMethodName = string.Empty;
        }
    }

    /// <summary>
    /// 表示一个枚举类型的生成配置。
    /// </summary>
    public sealed class EnumSchema
    {
        /// <summary>
        /// 枚举名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 枚举底层类型。
        /// </summary>
        public string UnderlyingType { get; set; }

        /// <summary>
        /// 枚举说明。
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// 枚举成员集合。
        /// </summary>
        public List<EnumMemberSchema> Members { get; set; }

        /// <summary>
        /// 初始化枚举配置。
        /// </summary>
        public EnumSchema()
        {
            Name = string.Empty;
            UnderlyingType = "byte";
            Desc = string.Empty;
            Members = new List<EnumMemberSchema>();
        }
    }

    /// <summary>
    /// 表示一个枚举成员的生成配置。
    /// </summary>
    public sealed class EnumMemberSchema
    {
        /// <summary>
        /// 枚举成员名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 枚举成员数值。
        /// </summary>
        public long Value { get; set; }

        /// <summary>
        /// 枚举成员说明。
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// 初始化枚举成员配置。
        /// </summary>
        public EnumMemberSchema()
        {
            Name = string.Empty;
            Desc = string.Empty;
        }
    }

    /// <summary>
    /// 表示一个可生成到协议类库中的自定义校验方法。
    /// </summary>
    public sealed class ValidationMethodSchema
    {
        /// <summary>
        /// 校验方法名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 校验方法体源码。
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// 初始化校验方法配置。
        /// </summary>
        public ValidationMethodSchema()
        {
            Name = string.Empty;
            Body = "return true;";
        }
    }

    /// <summary>
    /// 表示一次代码生成调用的输入请求。
    /// </summary>
    public sealed class GenerateRequest
    {
        /// <summary>
        /// 协议生成配置。
        /// </summary>
        public ProtocolSchema Schema { get; set; }

        /// <summary>
        /// 生成结果输出方式。
        /// </summary>
        public GenerateOutputMode OutputMode { get; set; }

        /// <summary>
        /// 本地写入模式下使用的输出根目录。
        /// </summary>
        public string OutputRootDirectory { get; set; }

        /// <summary>
        /// 初始化生成请求，并创建默认协议配置。
        /// </summary>
        public GenerateRequest()
        {
            Schema = new ProtocolSchema();
            OutputRootDirectory = string.Empty;
        }
    }

    /// <summary>
    /// 表示一次代码生成调用的输出结果。
    /// </summary>
    public sealed class GenerationResult
    {
        /// <summary>
        /// 生成是否成功。
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 生成出的文件集合。
        /// </summary>
        public List<GeneratedFile> Files { get; set; }

        /// <summary>
        /// 生成或校验过程中产生的诊断信息。
        /// </summary>
        public List<string> Diagnostics { get; set; }

        /// <summary>
        /// 生成文件打包后的 ZIP 字节。
        /// </summary>
        public byte[] ZipBytes { get; set; }

        /// <summary>
        /// 本地写入模式下实际写入的输出目录。
        /// </summary>
        public string LocalOutputDirectory { get; set; }

        /// <summary>
        /// 初始化生成结果集合。
        /// </summary>
        public GenerationResult()
        {
            Files = new List<GeneratedFile>();
            Diagnostics = new List<string>();
            ZipBytes = new byte[0];
            LocalOutputDirectory = string.Empty;
        }
    }

    /// <summary>
    /// 表示一个生成出的文件。
    /// </summary>
    public sealed class GeneratedFile
    {
        /// <summary>
        /// 文件在生成项目中的相对路径。
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 文件文本内容。
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 初始化空的生成文件。
        /// </summary>
        public GeneratedFile()
        {
            Path = string.Empty;
            Content = string.Empty;
        }

        /// <summary>
        /// 使用指定路径和内容初始化生成文件。
        /// </summary>
        /// <param name="path">文件在生成项目中的相对路径。</param>
        /// <param name="content">文件文本内容。</param>
        public GeneratedFile(string path, string content)
        {
            Path = path;
            Content = content;
        }
    }

    /// <summary>
    /// 表示协议配置校验结果。
    /// </summary>
    public sealed class SchemaValidationResult
    {
        /// <summary>
        /// 获取协议配置是否通过校验。
        /// </summary>
        public bool IsValid { get { return Errors.Count == 0; } }

        /// <summary>
        /// 校验错误集合。
        /// </summary>
        public List<string> Errors { get; private set; }

        /// <summary>
        /// 初始化协议配置校验结果。
        /// </summary>
        public SchemaValidationResult()
        {
            Errors = new List<string>();
        }
    }
}
