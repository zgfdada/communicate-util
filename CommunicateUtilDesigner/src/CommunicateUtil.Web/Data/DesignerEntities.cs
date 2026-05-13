using SqlSugar;

namespace CommunicateUtil.Web.Data;

[SugarTable("ProtocolProjects")]
public class ProtocolProjectEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public string ProjectName { get; set; } = "GeneratedCommunicateModels";
    public string Namespace { get; set; } = "GeneratedCommunicateModels";
    public string AssemblyName { get; set; } = "GeneratedCommunicateModels";
    public string TargetFramework { get; set; } = "netstandard2.0";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[SugarTable("CommClasses")]
public class CommClassEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
}

[SugarTable("CommFields")]
public class CommFieldEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int ClassId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TypeKind { get; set; } = "Basic";
    public string TypeName { get; set; } = "byte";
    public string CollectionKind { get; set; } = "None";
    public string OrderIndex { get; set; } = "0";
    public int StartIndex { get; set; } = -1;
    public string ArrayLength { get; set; } = "-1";
    public string AutoLengthType { get; set; } = string.Empty;
    public string EnumEndType { get; set; } = string.Empty;
    public string EndianType { get; set; } = "Big_ABCD";
    public string Desc { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string ValidationMethodName { get; set; } = string.Empty;
}

[SugarTable("EnumDefinitions")]
public class EnumDefinitionEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UnderlyingType { get; set; } = "byte";
    public string Desc { get; set; } = string.Empty;
}

[SugarTable("EnumMembers")]
public class EnumMemberEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int EnumId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Value { get; set; }
}

[SugarTable("ValidationMethods")]
public class ValidationMethodEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    [SugarColumn(Length = 4000)]
    public string Body { get; set; } = "return true;";
}
