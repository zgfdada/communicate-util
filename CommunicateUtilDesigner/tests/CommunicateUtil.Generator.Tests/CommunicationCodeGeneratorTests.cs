using CommunicateUtil.Generator.Models;

namespace CommunicateUtil.Generator.Tests;

public class CommunicationCodeGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_CreatesNetStandardProjectAndBasicClass()
    {
        var schema = CreateSchema();
        var generator = new CommunicationCodeGenerator();

        var result = await generator.GenerateAsync(new GenerateRequest { Schema = schema });

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        var projectFile = Assert.Single(result.Files.Where(f => f.Path == "DeviceProtocolModels.csproj"));
        Assert.Contains("<TargetFramework>netstandard2.0</TargetFramework>", projectFile.Content);
        var classFile = Assert.Single(result.Files.Where(f => f.Path == "Models/DeviceData.cs"));
        Assert.Contains("public class DeviceData : BaseCommunicateArrtObject", classFile.Content);
        Assert.Contains("[CommunicateArrtibute(OrderIndex = 1)]", classFile.Content);
        Assert.Contains("public byte DeviceId { get; set; }", classFile.Content);
    }

    [Fact]
    public async Task GenerateAsync_CreatesArraysListsEnumsNestedClassesAndValidation()
    {
        var schema = CreateSchema();
        schema.Enums.Add(new EnumSchema
        {
            Name = "CommandType",
            UnderlyingType = "byte",
            Desc = "命令类型",
            Members =
            {
                new EnumMemberSchema { Name = "Read", Value = 1 },
                new EnumMemberSchema { Name = "Write", Value = 2 }
            }
        });
        schema.Classes.Add(new CommClassSchema
        {
            Name = "DataPoint",
            Fields =
            {
                new CommFieldSchema { Name = "PointId", TypeName = "ushort", OrderIndex = "1" }
            }
        });
        schema.Classes[0].Fields.Add(new CommFieldSchema
        {
            Name = "Name",
            TypeKind = FieldTypeKind.String,
            TypeName = "string",
            OrderIndex = "2.1",
            ArrayLength = "32"
        });
        schema.Classes[0].Fields.Add(new CommFieldSchema
        {
            Name = "Commands",
            TypeKind = FieldTypeKind.Enum,
            TypeName = "CommandType",
            CollectionKind = FieldCollectionKind.List,
            OrderIndex = "3",
            AutoLengthType = "byte",
            EnumEndType = "byte"
        });
        schema.Classes[0].Fields.Add(new CommFieldSchema
        {
            Name = "Points",
            TypeKind = FieldTypeKind.CommClass,
            TypeName = "DataPoint",
            CollectionKind = FieldCollectionKind.Array,
            OrderIndex = "4",
            AutoLengthType = "byte",
            ValidationMethodName = "ValidatePoints"
        });
        schema.ValidationMethods.Add(new ValidationMethodSchema
        {
            Name = "ValidatePoints",
            Body = "return target != null;"
        });

        var result = await new CommunicationCodeGenerator().GenerateAsync(new GenerateRequest { Schema = schema });

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        var classFile = result.Files.Single(f => f.Path == "Models/DeviceData.cs").Content;
        Assert.Contains("ArrayLength = \"32\"", classFile);
        Assert.Contains("public string Name { get; set; }", classFile);
        Assert.Contains("public List<CommandType> Commands { get; set; }", classFile);
        Assert.Contains("public DataPoint[] Points { get; set; }", classFile);
        Assert.Contains("[ValidCheckArrtibute(\"DeviceProtocolModels\", \"ValidatePoints\")]", classFile);
        var enumFile = result.Files.Single(f => f.Path == "Enums/CommandType.cs").Content;
        Assert.Contains("/// 命令类型", enumFile);
        Assert.Contains("Validation/ProtocolValidationMethods.cs", result.Files.Select(f => f.Path));
        Assert.NotEmpty(result.ZipBytes);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsDiagnosticsForInvalidSchema()
    {
        var schema = CreateSchema();
        schema.Classes[0].Fields.Add(new CommFieldSchema { Name = "DeviceId", OrderIndex = "1" });

        var result = await new CommunicationCodeGenerator().GenerateAsync(new GenerateRequest { Schema = schema });

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Contains("字段名称重复"));
    }

    [Fact]
    public async Task GenerateClassAsync_CreatesOnlySelectedClassFile()
    {
        var schema = CreateSchema();
        schema.Classes.Add(new CommClassSchema
        {
            Name = "OtherData",
            Fields =
            {
                new CommFieldSchema { Name = "Value", TypeName = "byte", OrderIndex = "1" }
            }
        });

        var result = await new CommunicationCodeGenerator().GenerateClassAsync(schema, "DeviceData");

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        var file = Assert.Single(result.Files);
        Assert.Equal("Models/DeviceData.cs", file.Path);
        Assert.Contains("public class DeviceData", file.Content);
        Assert.DoesNotContain("OtherData", file.Content);
    }

    [Fact]
    public async Task GenerateEnumAsync_CreatesOnlySelectedEnumFile()
    {
        var schema = CreateSchema();
        schema.Enums.Add(new EnumSchema
        {
            Name = "CommandType",
            UnderlyingType = "byte",
            Desc = "命令类型",
            Members =
            {
                new EnumMemberSchema { Name = "Read", Value = 1 }
            }
        });

        var result = await new CommunicationCodeGenerator().GenerateEnumAsync(schema, "CommandType");

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        var file = Assert.Single(result.Files);
        Assert.Equal("Enums/CommandType.cs", file.Path);
        Assert.Contains("/// 命令类型", file.Content);
        Assert.Contains("public enum CommandType : byte", file.Content);
    }

    private static ProtocolSchema CreateSchema()
    {
        return new ProtocolSchema
        {
            ProjectName = "DeviceProtocolModels",
            Namespace = "Device.Protocol.Models",
            AssemblyName = "DeviceProtocolModels",
            TargetFramework = "netstandard2.0",
            Classes =
            {
                new CommClassSchema
                {
                    Name = "DeviceData",
                    Fields =
                    {
                        new CommFieldSchema
                        {
                            Name = "DeviceId",
                            TypeKind = FieldTypeKind.Basic,
                            TypeName = "byte",
                            OrderIndex = "1"
                        }
                    }
                }
            }
        };
    }
}
