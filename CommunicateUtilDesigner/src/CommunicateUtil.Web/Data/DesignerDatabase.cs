using SqlSugar;

namespace CommunicateUtil.Web.Data;

public static class DesignerDatabase
{
    public static void Initialize(ISqlSugarClient db)
    {
        db.CodeFirst.InitTables(
            typeof(ProtocolProjectEntity),
            typeof(CommClassEntity),
            typeof(CommFieldEntity),
            typeof(EnumDefinitionEntity),
            typeof(EnumMemberEntity),
            typeof(ValidationMethodEntity));
    }
}
