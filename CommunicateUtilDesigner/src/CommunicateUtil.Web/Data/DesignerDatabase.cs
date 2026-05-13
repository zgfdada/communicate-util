using SqlSugar;

namespace CommunicateUtil.Web.Data;

/// <summary>
/// 提供设计器 SQLite 数据库的表结构初始化入口。
/// </summary>
public static class DesignerDatabase
{
    /// <summary>
    /// 使用 SqlSugar CodeFirst 初始化设计器所需的数据表。
    /// </summary>
    /// <param name="db">SqlSugar 数据库客户端。</param>
    public static void Initialize(ISqlSugarClient db)
    {
        // 所有表都由实体定义驱动，启动时初始化可降低首次运行的手动配置成本。
        db.CodeFirst.InitTables(
            typeof(ProtocolProjectEntity),
            typeof(CommClassEntity),
            typeof(CommFieldEntity),
            typeof(EnumDefinitionEntity),
            typeof(EnumMemberEntity),
            typeof(ValidationMethodEntity));
    }
}
