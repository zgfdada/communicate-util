using SqlSugar;

namespace CommunicateUtil.Web.Data;

/// <summary>
/// 表示一个通讯协议生成项目。
/// </summary>
[SugarTable("ProtocolProjects")]
public class ProtocolProjectEntity
{
    /// <summary>
    /// 项目主键 ID。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 生成类库的项目名称。
    /// </summary>
    public string ProjectName { get; set; } = "GeneratedCommunicateModels";

    /// <summary>
    /// 生成代码的根命名空间。
    /// </summary>
    public string Namespace { get; set; } = "GeneratedCommunicateModels";

    /// <summary>
    /// 生成类库的程序集名称。
    /// </summary>
    public string AssemblyName { get; set; } = "GeneratedCommunicateModels";

    /// <summary>
    /// 生成类库的目标框架。
    /// </summary>
    public string TargetFramework { get; set; } = "netstandard2.0";

    /// <summary>
    /// 项目创建时间。
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 项目最后更新时间。
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 表示协议项目中的一个通讯类定义。
/// </summary>
[SugarTable("CommClasses")]
public class CommClassEntity
{
    /// <summary>
    /// 通讯类主键 ID。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 所属协议项目 ID。
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// 通讯类名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 通讯类说明。
    /// </summary>
    public string Desc { get; set; } = string.Empty;
}

/// <summary>
/// 表示通讯类中的一个字段定义。
/// </summary>
[SugarTable("CommFields")]
public class CommFieldEntity
{
    /// <summary>
    /// 字段主键 ID。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 所属协议项目 ID。
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// 所属通讯类 ID。
    /// </summary>
    public int ClassId { get; set; }

    /// <summary>
    /// 字段名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 字段类型分类。
    /// </summary>
    public string TypeKind { get; set; } = "Basic";

    /// <summary>
    /// 字段类型名称。
    /// </summary>
    public string TypeName { get; set; } = "byte";

    /// <summary>
    /// 字段集合类型。
    /// </summary>
    public string CollectionKind { get; set; } = "None";

    /// <summary>
    /// 字段在通讯序列中的顺序。
    /// </summary>
    public string OrderIndex { get; set; } = "0";

    /// <summary>
    /// 固定起始字节索引，-1 表示不指定。
    /// </summary>
    public int StartIndex { get; set; } = -1;

    /// <summary>
    /// 数组或字符串长度配置。
    /// </summary>
    public string ArrayLength { get; set; } = "-1";

    /// <summary>
    /// 自动长度字段类型。
    /// </summary>
    public string AutoLengthType { get; set; } = string.Empty;

    /// <summary>
    /// 枚举结束标记类型。
    /// </summary>
    public string EnumEndType { get; set; } = string.Empty;

    /// <summary>
    /// 字节序配置。
    /// </summary>
    public string EndianType { get; set; } = "Big_ABCD";

    /// <summary>
    /// 字段说明。
    /// </summary>
    public string Desc { get; set; } = string.Empty;

    /// <summary>
    /// 字段备注。
    /// </summary>
    public string Remarks { get; set; } = string.Empty;

    /// <summary>
    /// 字段绑定的自定义校验方法名称。
    /// </summary>
    public string ValidationMethodName { get; set; } = string.Empty;
}

/// <summary>
/// 表示协议项目中的一个枚举定义。
/// </summary>
[SugarTable("EnumDefinitions")]
public class EnumDefinitionEntity
{
    /// <summary>
    /// 枚举主键 ID。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 所属协议项目 ID。
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// 枚举名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 枚举底层类型。
    /// </summary>
    public string UnderlyingType { get; set; } = "byte";

    /// <summary>
    /// 枚举说明。
    /// </summary>
    public string Desc { get; set; } = string.Empty;
}

/// <summary>
/// 表示枚举定义中的一个枚举成员。
/// </summary>
[SugarTable("EnumMembers")]
public class EnumMemberEntity
{
    /// <summary>
    /// 枚举成员主键 ID。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 所属协议项目 ID。
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// 所属枚举 ID。
    /// </summary>
    public int EnumId { get; set; }

    /// <summary>
    /// 枚举成员名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 枚举成员数值。
    /// </summary>
    public long Value { get; set; }

    /// <summary>
    /// 枚举成员说明。
    /// </summary>
    public string Desc { get; set; } = string.Empty;
}

/// <summary>
/// 表示协议项目中的自定义字段校验方法。
/// </summary>
[SugarTable("ValidationMethods")]
public class ValidationMethodEntity
{
    /// <summary>
    /// 校验方法主键 ID。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 所属协议项目 ID。
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// 校验方法名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 校验方法体源码。
    /// </summary>
    [SugarColumn(Length = 4000)]
    public string Body { get; set; } = "return true;";
}
