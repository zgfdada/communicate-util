using CommunicateUtil.Generator.Models;
using CommunicateUtil.Web.Data;
using SqlSugar;

namespace CommunicateUtil.Web.Services;

public interface IDesignerRepository
{
    Task<List<ProtocolProjectEntity>> GetProjectsAsync();
    Task<ProtocolProjectEntity?> GetProjectAsync(int id);
    Task<int> CreateProjectAsync(ProtocolProjectEntity project);
    Task DeleteProjectAsync(int id);
    Task<ProjectWorkspaceViewModel?> GetWorkspaceAsync(int projectId);
    Task AddClassAsync(CommClassEntity entity);
    Task DeleteClassAsync(int projectId, int classId);
    Task AddFieldAsync(CommFieldEntity entity);
    Task UpdateFieldAsync(CommFieldEntity entity);
    Task DeleteFieldAsync(int projectId, int fieldId);
    Task MoveFieldAsync(int projectId, int fieldId, int direction);
    Task<int> AddEnumAsync(EnumDefinitionEntity entity);
    Task UpdateEnumAsync(EnumDefinitionEntity entity);
    Task AddEnumMemberAsync(EnumMemberEntity entity);
    Task UpdateEnumMemberAsync(EnumMemberEntity entity);
    Task DeleteEnumAsync(int projectId, int enumId);
    Task DeleteEnumMemberAsync(int projectId, int memberId);
    Task AddValidationMethodAsync(ValidationMethodEntity entity);
    Task DeleteValidationMethodAsync(int projectId, int methodId);
    Task<ProtocolSchema?> BuildSchemaAsync(int projectId);
}

public sealed class DesignerRepository : IDesignerRepository
{
    private readonly ISqlSugarClient _db;

    public DesignerRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task<List<ProtocolProjectEntity>> GetProjectsAsync()
    {
        return _db.Queryable<ProtocolProjectEntity>().OrderByDescending(p => p.UpdatedAt).ToListAsync();
    }

    public async Task<ProtocolProjectEntity?> GetProjectAsync(int id)
    {
        return await _db.Queryable<ProtocolProjectEntity>().FirstAsync(p => p.Id == id);
    }

    public async Task<int> CreateProjectAsync(ProtocolProjectEntity project)
    {
        project.TargetFramework = "netstandard2.0";
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;
        return await _db.Insertable(project).ExecuteReturnIdentityAsync();
    }

    public async Task DeleteProjectAsync(int id)
    {
        await _db.Deleteable<CommFieldEntity>().Where(f => f.ProjectId == id).ExecuteCommandAsync();
        await _db.Deleteable<CommClassEntity>().Where(c => c.ProjectId == id).ExecuteCommandAsync();
        await _db.Deleteable<EnumMemberEntity>().Where(e => e.ProjectId == id).ExecuteCommandAsync();
        await _db.Deleteable<EnumDefinitionEntity>().Where(e => e.ProjectId == id).ExecuteCommandAsync();
        await _db.Deleteable<ValidationMethodEntity>().Where(v => v.ProjectId == id).ExecuteCommandAsync();
        await _db.Deleteable<ProtocolProjectEntity>().Where(p => p.Id == id).ExecuteCommandAsync();
    }

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
            field.ArrayLength = NormalizeArrayLength(field.ArrayLength);
            field.AutoLengthType = ResolveAutoLengthType(field.CollectionKind, field.ArrayLength);
        }

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

    public async Task AddClassAsync(CommClassEntity entity)
    {
        await _db.Insertable(entity).ExecuteCommandAsync();
        await TouchProjectAsync(entity.ProjectId);
    }

    public async Task DeleteClassAsync(int projectId, int classId)
    {
        await _db.Deleteable<CommFieldEntity>().Where(f => f.ProjectId == projectId && f.ClassId == classId).ExecuteCommandAsync();
        await _db.Deleteable<CommClassEntity>().Where(c => c.ProjectId == projectId && c.Id == classId).ExecuteCommandAsync();
        await TouchProjectAsync(projectId);
    }

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

    public async Task UpdateFieldAsync(CommFieldEntity entity)
    {
        NormalizeField(entity);
        var existing = await _db.Queryable<CommFieldEntity>().FirstAsync(f => f.ProjectId == entity.ProjectId && f.Id == entity.Id);
        if (existing == null)
        {
            return;
        }

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

    public async Task DeleteFieldAsync(int projectId, int fieldId)
    {
        var field = await _db.Queryable<CommFieldEntity>().FirstAsync(f => f.ProjectId == projectId && f.Id == fieldId);
        await _db.Deleteable<CommFieldEntity>().Where(f => f.ProjectId == projectId && f.Id == fieldId).ExecuteCommandAsync();
        if (field != null)
        {
            await ReindexFieldsAsync(projectId, field.ClassId);
        }
        await TouchProjectAsync(projectId);
    }

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
            return;
        }

        var item = fields[index];
        fields[index] = fields[targetIndex];
        fields[targetIndex] = item;
        await ReindexFieldsAsync(projectId, current.ClassId, fields);
        await TouchProjectAsync(projectId);
    }

    public async Task<int> AddEnumAsync(EnumDefinitionEntity entity)
    {
        NormalizeEnum(entity);
        var id = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
        await TouchProjectAsync(entity.ProjectId);
        return id;
    }

    public async Task UpdateEnumAsync(EnumDefinitionEntity entity)
    {
        NormalizeEnum(entity);
        await _db.Updateable(entity)
            .UpdateColumns(e => new { e.Name, e.UnderlyingType, e.Desc })
            .Where(e => e.ProjectId == entity.ProjectId && e.Id == entity.Id)
            .ExecuteCommandAsync();
        await TouchProjectAsync(entity.ProjectId);
    }

    public async Task AddEnumMemberAsync(EnumMemberEntity entity)
    {
        NormalizeEnumMember(entity);
        await ShiftEnumMemberValuesAsync(entity.ProjectId, entity.EnumId, entity.Value);
        await _db.Insertable(entity).ExecuteCommandAsync();
        await TouchProjectAsync(entity.ProjectId);
    }

    public async Task UpdateEnumMemberAsync(EnumMemberEntity entity)
    {
        NormalizeEnumMember(entity);
        var existing = await _db.Queryable<EnumMemberEntity>().FirstAsync(e => e.ProjectId == entity.ProjectId && e.Id == entity.Id);
        if (existing == null)
        {
            return;
        }

        entity.EnumId = existing.EnumId;
        await ShiftEnumMemberValuesAsync(entity.ProjectId, entity.EnumId, entity.Value, entity.Id);
        await _db.Updateable(entity)
            .UpdateColumns(e => new { e.Name, e.Value, e.Desc })
            .Where(e => e.ProjectId == entity.ProjectId && e.Id == entity.Id)
            .ExecuteCommandAsync();
        await TouchProjectAsync(entity.ProjectId);
    }

    public async Task DeleteEnumAsync(int projectId, int enumId)
    {
        await _db.Deleteable<EnumMemberEntity>().Where(e => e.ProjectId == projectId && e.EnumId == enumId).ExecuteCommandAsync();
        await _db.Deleteable<EnumDefinitionEntity>().Where(e => e.ProjectId == projectId && e.Id == enumId).ExecuteCommandAsync();
        await TouchProjectAsync(projectId);
    }

    public async Task DeleteEnumMemberAsync(int projectId, int memberId)
    {
        await _db.Deleteable<EnumMemberEntity>().Where(e => e.ProjectId == projectId && e.Id == memberId).ExecuteCommandAsync();
        await TouchProjectAsync(projectId);
    }

    public async Task AddValidationMethodAsync(ValidationMethodEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Body))
        {
            entity.Body = "return true;";
        }
        await _db.Insertable(entity).ExecuteCommandAsync();
        await TouchProjectAsync(entity.ProjectId);
    }

    public async Task DeleteValidationMethodAsync(int projectId, int methodId)
    {
        await _db.Deleteable<ValidationMethodEntity>().Where(v => v.ProjectId == projectId && v.Id == methodId).ExecuteCommandAsync();
        await TouchProjectAsync(projectId);
    }

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

    private static CommFieldSchema ToFieldSchema(CommFieldEntity field)
    {
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

    private Task TouchProjectAsync(int projectId)
    {
        return _db.Updateable<ProtocolProjectEntity>()
            .SetColumns(p => p.UpdatedAt == DateTime.UtcNow)
            .Where(p => p.Id == projectId)
            .ExecuteCommandAsync();
    }

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

    private async Task ReindexFieldsAsync(int projectId, int classId, List<CommFieldEntity>? orderedFields = null)
    {
        var fields = orderedFields ?? await GetOrderedFieldsAsync(projectId, classId);
        for (var i = 0; i < fields.Count; i++)
        {
            var order = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            await _db.Updateable<CommFieldEntity>()
                .SetColumns(f => f.OrderIndex == order)
                .Where(f => f.ProjectId == projectId && f.Id == fields[i].Id)
                .ExecuteCommandAsync();
            fields[i].OrderIndex = order;
        }
    }

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

    private static float ParseOrderIndex(CommFieldEntity field)
    {
        return ParseOrderValue(field.OrderIndex);
    }

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

    private static void NormalizeField(CommFieldEntity entity)
    {
        entity.Name = (entity.Name ?? string.Empty).Trim();
        entity.TypeKind = string.IsNullOrWhiteSpace(entity.TypeKind) ? "Basic" : entity.TypeKind.Trim();
        entity.TypeName = string.IsNullOrWhiteSpace(entity.TypeName) ? "byte" : entity.TypeName.Trim();
        if (string.Equals(entity.TypeKind, "String", StringComparison.OrdinalIgnoreCase))
        {
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

    private static void NormalizeEnum(EnumDefinitionEntity entity)
    {
        entity.Name = (entity.Name ?? string.Empty).Trim();
        entity.UnderlyingType = string.IsNullOrWhiteSpace(entity.UnderlyingType)
            ? "byte"
            : entity.UnderlyingType.Trim();
        entity.Desc = entity.Desc ?? string.Empty;
    }

    private static void NormalizeEnumMember(EnumMemberEntity entity)
    {
        entity.Name = (entity.Name ?? string.Empty).Trim();
        entity.Desc = entity.Desc ?? string.Empty;
    }

    private static string ResolveAutoLengthType(FieldCollectionKind collectionKind, string arrayLength)
    {
        return collectionKind != FieldCollectionKind.None
            ? "byte"
            : string.Empty;
    }

    private static string ResolveAutoLengthType(string collectionKind, string arrayLength)
    {
        return !string.Equals(collectionKind, "None", StringComparison.OrdinalIgnoreCase)
            ? "byte"
            : string.Empty;
    }

    private static string NormalizeArrayLength(string arrayLength)
    {
        if (string.IsNullOrWhiteSpace(arrayLength))
        {
            return "-1";
        }

        var trimmed = arrayLength.Trim();
        return string.Equals(trimmed, "自动", StringComparison.OrdinalIgnoreCase)
            ? "-1"
            : trimmed;
    }
}

public sealed class ProjectWorkspaceViewModel
{
    public ProtocolProjectEntity Project { get; set; } = new();
    public List<ProtocolProjectEntity> Projects { get; set; } = new();
    public List<CommClassEntity> Classes { get; set; } = new();
    public List<CommFieldEntity> Fields { get; set; } = new();
    public List<EnumDefinitionEntity> Enums { get; set; } = new();
    public List<EnumMemberEntity> EnumMembers { get; set; } = new();
    public List<ValidationMethodEntity> ValidationMethods { get; set; } = new();
    public int? SelectedClassId { get; set; }
    public CommClassEntity? SelectedClass => SelectedClassId.HasValue
        ? Classes.FirstOrDefault(c => c.Id == SelectedClassId.Value)
        : null;
    public int? SelectedEnumId { get; set; }
    public EnumDefinitionEntity? SelectedEnum => SelectedEnumId.HasValue
        ? Enums.FirstOrDefault(e => e.Id == SelectedEnumId.Value)
        : null;

    public static readonly string[] BasicTypes =
    {
        "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
        "float", "double", "bool", "char"
    };

    public static readonly string[] LengthTypes =
    {
        "", "byte", "short", "ushort", "int", "uint"
    };

    public static readonly string[] EnumUnderlyingTypes =
    {
        "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong"
    };

    public static readonly string[] EndianTypes =
    {
        "Big_ABCD", "Little_DCBA", "LittleSwap_CDAB", "BigSwap_BADC"
    };
}
