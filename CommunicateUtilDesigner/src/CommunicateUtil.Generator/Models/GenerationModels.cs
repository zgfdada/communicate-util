using System.Collections.Generic;

namespace CommunicateUtil.Generator.Models
{
    public enum FieldTypeKind
    {
        Basic = 0,
        String = 1,
        Enum = 2,
        CommClass = 3
    }

    public enum FieldCollectionKind
    {
        None = 0,
        Array = 1,
        List = 2
    }

    public enum GenerateOutputMode
    {
        Preview = 0,
        DownloadZip = 1,
        WriteLocal = 2
    }

    public sealed class ProtocolSchema
    {
        public string ProjectName { get; set; }
        public string Namespace { get; set; }
        public string AssemblyName { get; set; }
        public string TargetFramework { get; set; }
        public List<CommClassSchema> Classes { get; set; }
        public List<EnumSchema> Enums { get; set; }
        public List<ValidationMethodSchema> ValidationMethods { get; set; }

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

    public sealed class CommClassSchema
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public List<CommFieldSchema> Fields { get; set; }

        public CommClassSchema()
        {
            Name = string.Empty;
            Desc = string.Empty;
            Fields = new List<CommFieldSchema>();
        }
    }

    public sealed class CommFieldSchema
    {
        public string Name { get; set; }
        public FieldTypeKind TypeKind { get; set; }
        public string TypeName { get; set; }
        public FieldCollectionKind CollectionKind { get; set; }
        public string OrderIndex { get; set; }
        public int StartIndex { get; set; }
        public string ArrayLength { get; set; }
        public string AutoLengthType { get; set; }
        public string EnumEndType { get; set; }
        public string EndianType { get; set; }
        public string Desc { get; set; }
        public string Remarks { get; set; }
        public string ValidationMethodName { get; set; }

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

    public sealed class EnumSchema
    {
        public string Name { get; set; }
        public string UnderlyingType { get; set; }
        public string Desc { get; set; }
        public List<EnumMemberSchema> Members { get; set; }

        public EnumSchema()
        {
            Name = string.Empty;
            UnderlyingType = "byte";
            Desc = string.Empty;
            Members = new List<EnumMemberSchema>();
        }
    }

    public sealed class EnumMemberSchema
    {
        public string Name { get; set; }
        public long Value { get; set; }
        public string Desc { get; set; }

        public EnumMemberSchema()
        {
            Name = string.Empty;
            Desc = string.Empty;
        }
    }

    public sealed class ValidationMethodSchema
    {
        public string Name { get; set; }
        public string Body { get; set; }

        public ValidationMethodSchema()
        {
            Name = string.Empty;
            Body = "return true;";
        }
    }

    public sealed class GenerateRequest
    {
        public ProtocolSchema Schema { get; set; }
        public GenerateOutputMode OutputMode { get; set; }
        public string OutputRootDirectory { get; set; }

        public GenerateRequest()
        {
            Schema = new ProtocolSchema();
            OutputRootDirectory = string.Empty;
        }
    }

    public sealed class GenerationResult
    {
        public bool Success { get; set; }
        public List<GeneratedFile> Files { get; set; }
        public List<string> Diagnostics { get; set; }
        public byte[] ZipBytes { get; set; }
        public string LocalOutputDirectory { get; set; }

        public GenerationResult()
        {
            Files = new List<GeneratedFile>();
            Diagnostics = new List<string>();
            ZipBytes = new byte[0];
            LocalOutputDirectory = string.Empty;
        }
    }

    public sealed class GeneratedFile
    {
        public string Path { get; set; }
        public string Content { get; set; }

        public GeneratedFile()
        {
            Path = string.Empty;
            Content = string.Empty;
        }

        public GeneratedFile(string path, string content)
        {
            Path = path;
            Content = content;
        }
    }

    public sealed class SchemaValidationResult
    {
        public bool IsValid { get { return Errors.Count == 0; } }
        public List<string> Errors { get; private set; }

        public SchemaValidationResult()
        {
            Errors = new List<string>();
        }
    }
}
