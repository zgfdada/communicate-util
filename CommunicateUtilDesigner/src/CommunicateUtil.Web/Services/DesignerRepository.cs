using CommunicateUtil.Generator.Models;
using CommunicateUtil.Web.Data;
using SqlSugar;

namespace CommunicateUtil.Web.Services;

/// <summary>
/// 定义设计器项目、类、字段、枚举和校验方法的数据访问能力。
/// </summary>
public interface IDesignerRepository
{
    /// <summary>
    /// 获取所有协议项目。
    /// </summary>
    /// <returns>按更新时间倒序排列的协议项目列表。</returns>
    Task<List<ProtocolProjectEntity>> GetProjectsAsync();

    /// <summary>
    /// 获取指定协议项目。
    /// </summary>
    /// <param name="id">协议项目 ID。</param>
    /// <returns>协议项目实体，不存在时返回 null。</returns>
    Task<ProtocolProjectEntity?> GetProjectAsync(int id);

    /// <summary>
    /// 创建协议项目。
    /// </summary>
    /// <param name="project">协议项目实体。</param>
    /// <returns>新项目 ID。</returns>
    Task<int> CreateProjectAsync(ProtocolProjectEntity project);

    /// <summary>
    /// 删除协议项目及其所有子数据。
    /// </summary>
    /// <param name="id">协议项目 ID。</param>
    Task DeleteProjectAsync(int id);

    /// <summary>
    /// 获取项目详情工作台所需的完整数据。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <returns>项目工作台视图模型，不存在时返回 null。</returns>
    Task<ProjectWorkspaceViewModel?> GetWorkspaceAsync(int projectId);

    /// <summary>
    /// 新增通讯类。
    /// </summary>
    /// <param name="entity">通讯类实体。</param>
    Task AddClassAsync(CommClassEntity entity);

    /// <summary>
    /// 删除通讯类及其字段。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="classId">通讯类 ID。</param>
    Task DeleteClassAsync(int projectId, int classId);

    /// <summary>
    /// 新增通讯字段。
    /// </summary>
    /// <param name="entity">通讯字段实体。</param>
    Task AddFieldAsync(CommFieldEntity entity);

    /// <summary>
    /// 更新通讯字段。
    /// </summary>
    /// <param name="entity">通讯字段实体。</param>
    Task UpdateFieldAsync(CommFieldEntity entity);

    /// <summary>
    /// 删除通讯字段。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="fieldId">字段 ID。</param>
    Task DeleteFieldAsync(int projectId, int fieldId);

    /// <summary>
    /// 上移或下移通讯字段。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="fieldId">字段 ID。</param>
    /// <param name="direction">移动方向，负数上移，正数下移。</param>
    Task MoveFieldAsync(int projectId, int fieldId, int direction);

    /// <summary>
    /// 新增枚举定义。
    /// </summary>
    /// <param name="entity">枚举定义实体。</param>
    /// <returns>新枚举 ID。</returns>
    Task<int> AddEnumAsync(EnumDefinitionEntity entity);

    /// <summary>
    /// 更新枚举定义。
    /// </summary>
    /// <param name="entity">枚举定义实体。</param>
    Task UpdateEnumAsync(EnumDefinitionEntity entity);

    /// <summary>
    /// 新增枚举成员。
    /// </summary>
    /// <param name="entity">枚举成员实体。</param>
    Task AddEnumMemberAsync(EnumMemberEntity entity);

    /// <summary>
    /// 更新枚举成员。
    /// </summary>
    /// <param name="entity">枚举成员实体。</param>
    Task UpdateEnumMemberAsync(EnumMemberEntity entity);

    /// <summary>
    /// 删除枚举定义及其成员。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="enumId">枚举 ID。</param>
    Task DeleteEnumAsync(int projectId, int enumId);

    /// <summary>
    /// 删除枚举成员。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="memberId">枚举成员 ID。</param>
    Task DeleteEnumMemberAsync(int projectId, int memberId);

    /// <summary>
    /// 新增自定义校验方法。
    /// </summary>
    /// <param name="entity">校验方法实体。</param>
    Task AddValidationMethodAsync(ValidationMethodEntity entity);

    /// <summary>
    /// 删除自定义校验方法。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="methodId">校验方法 ID。</param>
    Task DeleteValidationMethodAsync(int projectId, int methodId);

    /// <summary>
    /// 将设计器持久化数据组装为代码生成器使用的协议配置。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <returns>协议配置，不存在时返回 null。</returns>
    Task<ProtocolSchema?> BuildSchemaAsync(int projectId);
}

/// <summary>
/// 基于 SqlSugar 实现设计器项目数据的增删改查和协议配置组装。
/// </summary>
public sealed class DesignerRepository : IDesignerRepository
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 初始化设计器数据仓储。
    /// </summary>
    /// <param name="db">SqlSugar 数据库客户端。</param>
    public DesignerRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public Task<List<ProtocolProjectEntity>> GetProjectsAsync()
    {
        return _db.Queryable<ProtocolProjectEntity>().OrderByDescending(p => p.UpdatedAt).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProtocolProjectEntity?> GetProjectAsync(int id)
    {
        return await _db.Queryable<ProtocolProjectEntity>().FirstAsync(p => p.Id == id);
    }

    /// <inheritdoc />
    public async Task<int> CreateProjectAsync(ProtocolProjectEntity project)
    {
        // 生成器只支持 netstandard2.0 输出，这里统一覆盖避免 UI 传入其他目标框架。
        project.TargetFramework = "netstandard2.0";
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;
        return await _db.Insertable(project).ExecuteReturnIdentityAsync();
    }

    /// <inheritdoc />
    public async Task DeleteProjectAsync(int id)
    {
        // SQLite 未配置级联删除时，按子表到主表的顺序手动清理项目关联数据。
        await _db.Deleteable<CommFieldEntity>().Where(f => f.ProjectId == id).ExecuteCommandAsync();
        await _db.Deleteable<CommClassEntity>().Where(c => c.ProjectId == id).ExecuteCommandAsync();
        await _db.Deleteable<EnumMemberEntity>().Where(e => e.ProjectId == id).ExecuteCommandAsync();
        await _db.Deleteable<EnumDefinitionEntity>().Where(e => e.ProjectId == id).ExecuteCommandAsync();
        await _db.Deleteable<ValidationMethodEntity>().Where(v => v.ProjectId == id).ExecuteCommandAsync();
        await _db.Deleteable<ProtocolProjectEntity>().Where(p => p.Id == id).ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public async Task<ProjectWorkspaceViewModel?> GetWorkspaceAsync(int projectId)
    {
        var project = await GetProjectAsync(projectId);
        if (project == null)
        {
            return null;
        }

        var fields = await _db.Queryable<CommFieldEntity>().Where(f => f.ProjectId == projectId).ToListAsync();
        foreach (var field in fields)
        {
            // 展示前统一规范长度和自动长度类型，使旧数据和新表单数据显示一致。
            field.ArrayLength = NormalizeArrayLength(field.ArrayLength);
            field.AutoLengthType = ResolveAutoLengthType(field.CollectionKind, field.ArrayLength);
        }

        // 字段按所属类和协议顺序排序，保证页面列表顺序与最终生成顺序一致。
        fields = fields
            .OrderBy(f => f.ClassId)
            .ThenBy(ParseOrderIndex)
            .ThenBy(f => f.Id)
            .ToList();

        return new ProjectWorkspaceViewModel
        {
            Project = project,
            Projects = await GetProjectsAsync(),
            Classes = await _db.Queryable<CommClassEntity>().Where(c => c.ProjectId == projectId).OrderBy(c => c.Name).ToListAsync(),
            Fields = fields,
            Enums = await _db.Queryable<EnumDefinitionEntity>().Where(e => e.ProjectId == projectId).OrderBy(e => e.Name).ToListAsync(),
            EnumMembers = (await _db.Queryable<EnumMemberEntity>().Where(e => e.ProjectId == projectId).ToListAsync())
                .OrderBy(e => e.EnumId)
                .ThenBy(e => e.Value)
                .ToList(),
            ValidationMethods = await _db.Queryable<ValidationMethodEntity>().Where(v => v.ProjectId == projectId).OrderBy(v => v.Name).ToListAsync()
        };
    }

    /// <inheritdoc />
    public async Task AddClassAsync(CommClassEntity entity)
    {
        await _db.Insertable(entity).ExecuteCommandAsync();
        await TouchProjectAsync(entity.ProjectId);
    }

    /// <inheritdoc />
    public async Task DeleteClassAsync(int projectId, int classId)
    {
        // 通讯类删除时同步删除字段，避免保留无父级的字段记录。
        await _db.Deleteable<CommFieldEntity>().Where(f => f.ProjectId == projectId && f.ClassId == classId).ExecuteCommandAsync();
        await _db.Deleteable<CommClassEntity>().Where(c => c.ProjectId == projectId && c.Id == classId).ExecuteCommandAsync();
        await TouchProjectAsync(projectId);
    }

    /// <inheritdoc />
    public async Task AddFieldAsync(CommFieldEntity entity)
    {
        NormalizeField(entity);
        var fields = await GetOrderedFieldsAsync(entity.ProjectId, entity.ClassId);
        var insertOrder = ParseOrderValue(entity.OrderIndex);
        if (insertOrder <= 0)
        {
            insertOrder = fields.Count + 1;
        }
        insertOrder = Math.Min(insertOrder, fields.Count + 1);
        // 插入到中间位置时，先把当前位置及之后的字段顺序后移，避免 OrderIndex 冲突。
        foreach (var field in fields.Where(f => ParseOrderIndex(f) >= insertOrder))
        {
            var nextOrder = (ParseOrderIndex(field) + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            await _db.Updateable<CommFieldEntity>()
                .SetColumns(f => f.OrderIndex == nextOrder)
                .Where(f => f.ProjectId == entity.ProjectId && f.Id == field.Id)
                .ExecuteCommandAsync();
        }
        entity.OrderIndex = insertOrder.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await _db.Insertable(entity).ExecuteCommandAsync();
        await ReindexFieldsAsync(entity.ProjectId, entity.ClassId);
        await TouchProjectAsync(entity.ProjectId);
    }

    /// <inheritdoc />
    public async Task UpdateFieldAsync(CommFieldEntity entity)
    {
        NormalizeField(entity);
        var existing = await _db.Queryable<CommFieldEntity>().FirstAsync(f => f.ProjectId == entity.ProjectId && f.Id == entity.Id);
        if (existing == null)
        {
            return;
        }

        // 更新字段时沿用数据库中的 ClassId，避免表单篡改导致字段跨类移动。
        entity.ClassId = existing.ClassId;
        await _db.Updateable(entity)
            .UpdateColumns(f => new
            {
                f.Name,
                f.TypeKind,
                f.TypeName,
                f.CollectionKind,
                f.OrderIndex,
                f.StartIndex,
                f.ArrayLength,
                f.AutoLengthType,
                f.EnumEndType,
                f.EndianType,
                f.Desc,
                f.Remarks,
                f.ValidationMethodName
            })
            .Where(f => f.ProjectId == entity.ProjectId && f.Id == entity.Id)
            .ExecuteCommandAsync();
        await ReindexFieldsAsync(entity.ProjectId, entity.ClassId);
        await TouchProjectAsync(entity.ProjectId);
    }

    /// <inheritdoc />
    public async Task DeleteFieldAsync(int projectId, int fieldId)
    {
        var field = await _db.Queryable<CommFieldEntity>().FirstAsync(f => f.ProjectId == projectId && f.Id == fieldId);
        await _db.Deleteable<CommFieldEntity>().Where(f => f.ProjectId == projectId && f.Id == fieldId).ExecuteCommandAsync();
        if (field != null)
        {
            // 删除字段后重新编号，保持当前通讯类字段顺序连续。
            await ReindexFieldsAsync(projectId, field.ClassId);
        }
        await TouchProjectAsync(projectId);
    }

    /// <inheritdoc />
    public async Task MoveFieldAsync(int projectId, int fieldId, int direction)
    {
        if (direction == 0)
        {
            return;
        }

        var current = await _db.Queryable<CommFieldEntity>().FirstAsync(f => f.ProjectId == projectId && f.Id == fieldId);
        if (current == null)
        {
            return;
        }

        var fields = await GetOrderedFieldsAsync(projectId, current.ClassId);
        var index = fields.FindIndex(f => f.Id == fieldId);
        var targetIndex = direction < 0 ? index - 1 : index + 1;
        if (index < 0 || targetIndex < 0 || targetIndex >= fields.Count)
        {
            // 已经在边界位置时不做任何移动，避免产生越界顺序。
            return;
        }

        // 通过交换内存列表中的相邻项，再统一重排 OrderIndex，保证顺序字段连续且稳定。
        var item = fields[index];
        fields[index] = fields[targetIndex];
        fields[targetIndex] = item;
        await ReindexFieldsAsync(projectId, current.ClassId, fields);
        await TouchProjectAsync(projectId);
    }

    /// <inheritdoc />
    public async Task<int> AddEnumAsync(EnumDefinitionEntity entity)
    {
        NormalizeEnum(entity);
        var id = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
        await TouchProjectAsync(entity.ProjectId);
        return id;
    }

    /// <inheritdoc />
    public async Task UpdateEnumAsync(EnumDefinitionEntity entity)
    {
        NormalizeEnum(entity);
        await _db.Updateable(entity)
            .UpdateColumns(e => new { e.Name, e.UnderlyingType, e.Desc })
            .Where(e => e.ProjectId == entity.ProjectId && e.Id == entity.Id)
            .ExecuteCommandAsync();
        await TouchProjectAsync(entity.ProjectId);
    }

    /// <inheritdoc />
    public async Task AddEnumMemberAsync(EnumMemberEntity entity)
    {
        NormalizeEnumMember(entity);
        // 新增枚举值时先为冲突或后续值腾位，避免同枚举内 Value 重复。
        await ShiftEnumMemberValuesAsync(entity.ProjectId, entity.EnumId, entity.Value);
        await _db.Insertable(entity).ExecuteCommandAsync();
        await TouchProjectAsync(entity.ProjectId);
    }

    /// <inheritdoc />
    public async Task UpdateEnumMemberAsync(EnumMemberEntity entity)
    {
        NormalizeEnumMember(entity);
        var existing = await _db.Queryable<EnumMemberEntity>().FirstAsync(e => e.ProjectId == entity.ProjectId && e.Id == entity.Id);
        if (existing == null)
        {
            return;
        }

        entity.EnumId = existing.EnumId;
        // 更新时排除当前枚举成员自身，只移动其他可能冲突的枚举值。
        await ShiftEnumMemberValuesAsync(entity.ProjectId, entity.EnumId, entity.Value, entity.Id);
        await _db.Updateable(entity)
            .UpdateColumns(e => new { e.Name, e.Value, e.Desc })
            .Where(e => e.ProjectId == entity.ProjectId && e.Id == entity.Id)
            .ExecuteCommandAsync();
        await TouchProjectAsync(entity.ProjectId);
    }

    /// <inheritdoc />
    public async Task DeleteEnumAsync(int projectId, int enumId)
    {
        // 枚举定义删除时同步删除成员，避免保留无父级的枚举值。
        await _db.Deleteable<EnumMemberEntity>().Where(e => e.ProjectId == projectId && e.EnumId == enumId).ExecuteCommandAsync();
        await _db.Deleteable<EnumDefinitionEntity>().Where(e => e.ProjectId == projectId && e.Id == enumId).ExecuteCommandAsync();
        await TouchProjectAsync(projectId);
    }

    /// <inheritdoc />
    public async Task DeleteEnumMemberAsync(int projectId, int memberId)
    {
        await _db.Deleteable<EnumMemberEntity>().Where(e => e.ProjectId == projectId && e.Id == memberId).ExecuteCommandAsync();
        await TouchProjectAsync(projectId);
    }

    /// <inheritdoc />
    public async Task AddValidationMethodAsync(ValidationMethodEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Body))
        {
            // 空方法体默认返回 true，保证后续生成的校验方法可以直接编译。
            entity.Body = "return true;";
        }
        await _db.Insertable(entity).ExecuteCommandAsync();
        await TouchProjectAsync(entity.ProjectId);
    }

    /// <inheritdoc />
    public async Task DeleteValidationMethodAsync(int projectId, int methodId)
    {
        await _db.Deleteable<ValidationMethodEntity>().Where(v => v.ProjectId == projectId && v.Id == methodId).ExecuteCommandAsync();
        await TouchProjectAsync(projectId);
    }

    /// <inheritdoc />
    public async Task<ProtocolSchema?> BuildSchemaAsync(int projectId)
    {
        var workspace = await GetWorkspaceAsync(projectId);
        if (workspace == null)
        {
            return null;
        }

        var schema = new ProtocolSchema
        {
            ProjectName = workspace.Project.ProjectName,
            Namespace = workspace.Project.Namespace,
            AssemblyName = workspace.Project.AssemblyName,
            TargetFramework = "netstandard2.0"
        };

        // 数据库实体先转换成生成器模型，避免 Web 层实体直接泄漏到 Generator 类库。
        foreach (var enumEntity in workspace.Enums)
        {
            schema.Enums.Add(new EnumSchema
            {
                Name = enumEntity.Name,
                UnderlyingType = enumEntity.UnderlyingType,
                Desc = enumEntity.Desc,
                Members = workspace.EnumMembers
                    .Where(m => m.EnumId == enumEntity.Id)
                    .Select(m => new EnumMemberSchema { Name = m.Name, Value = m.Value, Desc = m.Desc })
                    .ToList()
            });
        }

        // 字段集合由工作台中已规范化和排序后的字段转换而来，保证生成结果稳定。
        foreach (var classEntity in workspace.Classes)
        {
            schema.Classes.Add(new CommClassSchema
            {
                Name = classEntity.Name,
                Desc = classEntity.Desc,
                Fields = workspace.Fields
                    .Where(f => f.ClassId == classEntity.Id)
                    .Select(ToFieldSchema)
                    .ToList()
            });
        }

        // 自定义校验方法随协议配置一起传递给生成器，由生成器负责输出静态校验方法文件。
        foreach (var method in workspace.ValidationMethods)
        {
            schema.ValidationMethods.Add(new ValidationMethodSchema
            {
                Name = method.Name,
                Body = method.Body
            });
        }

        return schema;
    }

    /// <summary>
    /// 将字段数据库实体转换为生成器字段配置。
    /// </summary>
    /// <param name="field">字段数据库实体。</param>
    /// <returns>生成器字段配置。</returns>
    private static CommFieldSchema ToFieldSchema(CommFieldEntity field)
    {
        // 字符串枚举值来自页面表单，转换失败时使用枚举默认值，后续生成器校验会给出诊断。
        Enum.TryParse<FieldTypeKind>(field.TypeKind, true, out var typeKind);
        Enum.TryParse<FieldCollectionKind>(field.CollectionKind, true, out var collectionKind);
        return new CommFieldSchema
        {
            Name = field.Name,
            TypeKind = typeKind,
            TypeName = field.TypeName,
            CollectionKind = collectionKind,
            OrderIndex = field.OrderIndex,
            StartIndex = field.StartIndex,
            ArrayLength = NormalizeArrayLength(field.ArrayLength),
            AutoLengthType = ResolveAutoLengthType(collectionKind, field.ArrayLength),
            EnumEndType = field.EnumEndType,
            EndianType = field.EndianType,
            Desc = field.Desc,
            Remarks = field.Remarks,
            ValidationMethodName = field.ValidationMethodName
        };
    }

    /// <summary>
    /// 更新协议项目的最后修改时间。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <returns>异步数据库操作。</returns>
    private Task TouchProjectAsync(int projectId)
    {
        return _db.Updateable<ProtocolProjectEntity>()
            .SetColumns(p => p.UpdatedAt == DateTime.UtcNow)
            .Where(p => p.Id == projectId)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取指定通讯类下按协议顺序排列的字段列表。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="classId">通讯类 ID。</param>
    /// <returns>字段列表。</returns>
    private async Task<List<CommFieldEntity>> GetOrderedFieldsAsync(int projectId, int classId)
    {
        var fields = await _db.Queryable<CommFieldEntity>()
            .Where(f => f.ProjectId == projectId && f.ClassId == classId)
            .ToListAsync();

        return fields
            .OrderBy(ParseOrderIndex)
            .ThenBy(f => f.Id)
            .ToList();
    }

    /// <summary>
    /// 重新整理指定通讯类下字段的连续顺序。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="classId">通讯类 ID。</param>
    /// <param name="orderedFields">可选的已排序字段列表。</param>
    /// <returns>异步数据库操作。</returns>
    private async Task ReindexFieldsAsync(int projectId, int classId, List<CommFieldEntity>? orderedFields = null)
    {
        var fields = orderedFields ?? await GetOrderedFieldsAsync(projectId, classId);
        for (var i = 0; i < fields.Count; i++)
        {
            // 使用 1 开始的连续序号，避免多次插入和移动后 OrderIndex 变得稀疏。
            var order = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            await _db.Updateable<CommFieldEntity>()
                .SetColumns(f => f.OrderIndex == order)
                .Where(f => f.ProjectId == projectId && f.Id == fields[i].Id)
                .ExecuteCommandAsync();
            fields[i].OrderIndex = order;
        }
    }

    /// <summary>
    /// 在新增或更新枚举成员时后移冲突的枚举值。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="enumId">枚举 ID。</param>
    /// <param name="requestedValue">请求写入的枚举值。</param>
    /// <param name="excludedMemberId">需要排除的当前枚举成员 ID。</param>
    /// <returns>异步数据库操作。</returns>
    private async Task ShiftEnumMemberValuesAsync(int projectId, int enumId, long requestedValue, int? excludedMemberId = null)
    {
        var members = await _db.Queryable<EnumMemberEntity>()
            .Where(e => e.ProjectId == projectId && e.EnumId == enumId)
            .ToListAsync();

        var shiftedMembers = members
            .Where(m => (!excludedMemberId.HasValue || m.Id != excludedMemberId.Value) && m.Value >= requestedValue)
            .OrderBy(m => m.Value)
            .ThenBy(m => m.Id)
            .ToList();

        var nextValue = requestedValue + 1;
        foreach (var member in shiftedMembers)
        {
            // 确保后续成员值至少比请求值大 1，并在连续冲突时继续递增。
            var shiftedValue = Math.Max(member.Value, nextValue);
            if (shiftedValue != member.Value)
            {
                await _db.Updateable<EnumMemberEntity>()
                    .SetColumns(e => e.Value == shiftedValue)
                    .Where(e => e.ProjectId == projectId && e.Id == member.Id)
                    .ExecuteCommandAsync();
            }

            nextValue = shiftedValue + 1;
        }
    }

    /// <summary>
    /// 解析字段实体中的顺序值。
    /// </summary>
    /// <param name="field">字段实体。</param>
    /// <returns>字段顺序数值。</returns>
    private static float ParseOrderIndex(CommFieldEntity field)
    {
        return ParseOrderValue(field.OrderIndex);
    }

    /// <summary>
    /// 将字段顺序字符串解析为浮点数。
    /// </summary>
    /// <param name="orderIndex">字段顺序字符串。</param>
    /// <returns>字段顺序数值，解析失败时返回 0。</returns>
    private static float ParseOrderValue(string orderIndex)
    {
        return float.TryParse(
            orderIndex,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : 0;
    }

    /// <summary>
    /// 规范化字段实体的默认值和空白文本。
    /// </summary>
    /// <param name="entity">字段实体。</param>
    private static void NormalizeField(CommFieldEntity entity)
    {
        entity.Name = (entity.Name ?? string.Empty).Trim();
        entity.TypeKind = string.IsNullOrWhiteSpace(entity.TypeKind) ? "Basic" : entity.TypeKind.Trim();
        entity.TypeName = string.IsNullOrWhiteSpace(entity.TypeName) ? "byte" : entity.TypeName.Trim();
        if (string.Equals(entity.TypeKind, "String", StringComparison.OrdinalIgnoreCase))
        {
            // 字符串字段强制使用 string，避免 TypeKind 和 TypeName 组合不一致。
            entity.TypeName = "string";
        }
        entity.CollectionKind = string.IsNullOrWhiteSpace(entity.CollectionKind) ? "None" : entity.CollectionKind.Trim();
        entity.OrderIndex = string.IsNullOrWhiteSpace(entity.OrderIndex) ? "0" : entity.OrderIndex.Trim();
        entity.ArrayLength = NormalizeArrayLength(entity.ArrayLength);
        entity.AutoLengthType = ResolveAutoLengthType(entity.CollectionKind, entity.ArrayLength);
        entity.EnumEndType = (entity.EnumEndType ?? string.Empty).Trim();
        entity.EndianType = string.IsNullOrWhiteSpace(entity.EndianType) ? "Big_ABCD" : entity.EndianType.Trim();
        entity.Desc = entity.Desc ?? string.Empty;
        entity.Remarks = entity.Remarks ?? string.Empty;
        entity.ValidationMethodName = (entity.ValidationMethodName ?? string.Empty).Trim();
    }

    /// <summary>
    /// 规范化枚举定义实体的默认值和空白文本。
    /// </summary>
    /// <param name="entity">枚举定义实体。</param>
    private static void NormalizeEnum(EnumDefinitionEntity entity)
    {
        entity.Name = (entity.Name ?? string.Empty).Trim();
        entity.UnderlyingType = string.IsNullOrWhiteSpace(entity.UnderlyingType)
            ? "byte"
            : entity.UnderlyingType.Trim();
        entity.Desc = entity.Desc ?? string.Empty;
    }

    /// <summary>
    /// 规范化枚举成员实体的默认值和空白文本。
    /// </summary>
    /// <param name="entity">枚举成员实体。</param>
    private static void NormalizeEnumMember(EnumMemberEntity entity)
    {
        entity.Name = (entity.Name ?? string.Empty).Trim();
        entity.Desc = entity.Desc ?? string.Empty;
    }

    /// <summary>
    /// 根据集合类型解析自动长度字段类型。
    /// </summary>
    /// <param name="collectionKind">字段集合类型。</param>
    /// <param name="arrayLength">数组长度配置。</param>
    /// <returns>自动长度字段类型。</returns>
    private static string ResolveAutoLengthType(FieldCollectionKind collectionKind, string arrayLength)
    {
        // 集合字段当前统一使用 byte 存储长度；非集合字段不生成自动长度配置。
        return collectionKind != FieldCollectionKind.None
            ? "byte"
            : string.Empty;
    }

    /// <summary>
    /// 根据集合类型文本解析自动长度字段类型。
    /// </summary>
    /// <param name="collectionKind">字段集合类型文本。</param>
    /// <param name="arrayLength">数组长度配置。</param>
    /// <returns>自动长度字段类型。</returns>
    private static string ResolveAutoLengthType(string collectionKind, string arrayLength)
    {
        return !string.Equals(collectionKind, "None", StringComparison.OrdinalIgnoreCase)
            ? "byte"
            : string.Empty;
    }

    /// <summary>
    /// 规范化数组或字符串长度配置。
    /// </summary>
    /// <param name="arrayLength">数组或字符串长度配置。</param>
    /// <returns>规范化后的长度配置。</returns>
    private static string NormalizeArrayLength(string arrayLength)
    {
        if (string.IsNullOrWhiteSpace(arrayLength))
        {
            return "-1";
        }

        var trimmed = arrayLength.Trim();
        // 页面上的“自动”表示由自动长度字段负责描述长度，生成器侧用 -1 表示不写固定长度。
        return string.Equals(trimmed, "自动", StringComparison.OrdinalIgnoreCase)
            ? "-1"
            : trimmed;
    }
}

/// <summary>
/// 表示项目详情页一次渲染所需的完整工作台数据。
/// </summary>
public sealed class ProjectWorkspaceViewModel
{
    /// <summary>
    /// 当前协议项目。
    /// </summary>
    public ProtocolProjectEntity Project { get; set; } = new();

    /// <summary>
    /// 左侧或顶部项目切换列表。
    /// </summary>
    public List<ProtocolProjectEntity> Projects { get; set; } = new();

    /// <summary>
    /// 当前项目下的通讯类集合。
    /// </summary>
    public List<CommClassEntity> Classes { get; set; } = new();

    /// <summary>
    /// 当前项目下的字段集合。
    /// </summary>
    public List<CommFieldEntity> Fields { get; set; } = new();

    /// <summary>
    /// 当前项目下的枚举集合。
    /// </summary>
    public List<EnumDefinitionEntity> Enums { get; set; } = new();

    /// <summary>
    /// 当前项目下的枚举成员集合。
    /// </summary>
    public List<EnumMemberEntity> EnumMembers { get; set; } = new();

    /// <summary>
    /// 当前项目下的自定义校验方法集合。
    /// </summary>
    public List<ValidationMethodEntity> ValidationMethods { get; set; } = new();

    /// <summary>
    /// 当前选中的通讯类 ID。
    /// </summary>
    public int? SelectedClassId { get; set; }

    /// <summary>
    /// 当前选中的通讯类。
    /// </summary>
    public CommClassEntity? SelectedClass => SelectedClassId.HasValue
        ? Classes.FirstOrDefault(c => c.Id == SelectedClassId.Value)
        : null;

    /// <summary>
    /// 当前选中的枚举 ID。
    /// </summary>
    public int? SelectedEnumId { get; set; }

    /// <summary>
    /// 当前选中的枚举。
    /// </summary>
    public EnumDefinitionEntity? SelectedEnum => SelectedEnumId.HasValue
        ? Enums.FirstOrDefault(e => e.Id == SelectedEnumId.Value)
        : null;

    /// <summary>
    /// 页面可选的基础字段类型集合。
    /// </summary>
    public static readonly string[] BasicTypes =
    {
        "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
        "float", "double", "bool", "char"
    };

    /// <summary>
    /// 页面可选的自动长度字段类型集合。
    /// </summary>
    public static readonly string[] LengthTypes =
    {
        "", "byte", "short", "ushort", "int", "uint"
    };

    /// <summary>
    /// 页面可选的枚举底层类型集合。
    /// </summary>
    public static readonly string[] EnumUnderlyingTypes =
    {
        "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong"
    };

    /// <summary>
    /// 页面可选的字节序类型集合。
    /// </summary>
    public static readonly string[] EndianTypes =
    {
        "Big_ABCD", "Little_DCBA", "LittleSwap_CDAB", "BigSwap_BADC"
    };
}
