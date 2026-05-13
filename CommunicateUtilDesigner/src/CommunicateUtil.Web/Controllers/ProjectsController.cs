using CommunicateUtil.Generator;
using CommunicateUtil.Generator.Models;
using CommunicateUtil.Web.Data;
using CommunicateUtil.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommunicateUtil.Web.Controllers;

/// <summary>
/// 处理通讯协议项目、通讯类、字段、枚举、校验方法和代码生成的页面请求。
/// </summary>
public class ProjectsController : Controller
{
    private readonly IDesignerRepository _repository;
    private readonly ICommunicationCodeGenerator _generator;
    private readonly DesignerOutputPaths _paths;

    /// <summary>
    /// 初始化项目设计器控制器。
    /// </summary>
    /// <param name="repository">设计器数据仓储。</param>
    /// <param name="generator">通讯类库代码生成器。</param>
    /// <param name="paths">设计器本地输出路径服务。</param>
    public ProjectsController(
        IDesignerRepository repository,
        ICommunicationCodeGenerator generator,
        DesignerOutputPaths paths)
    {
        _repository = repository;
        _generator = generator;
        _paths = paths;
    }

    /// <summary>
    /// 显示协议项目列表。
    /// </summary>
    /// <returns>协议项目列表视图。</returns>
    public async Task<IActionResult> Index()
    {
        return View(await _repository.GetProjectsAsync());
    }

    /// <summary>
    /// 显示创建协议项目页面。
    /// </summary>
    /// <returns>创建项目视图。</returns>
    public IActionResult Create()
    {
        return View(new ProtocolProjectEntity());
    }

    /// <summary>
    /// 创建协议项目，并跳转到项目详情页。
    /// </summary>
    /// <param name="project">协议项目实体。</param>
    /// <returns>项目详情页重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProtocolProjectEntity project)
    {
        // 生成器当前固定生成 netstandard2.0 类库，创建项目时直接锁定目标框架。
        project.TargetFramework = "netstandard2.0";
        if (string.IsNullOrWhiteSpace(project.AssemblyName))
        {
            project.AssemblyName = project.ProjectName;
        }
        if (string.IsNullOrWhiteSpace(project.Namespace))
        {
            project.Namespace = project.ProjectName;
        }

        var id = await _repository.CreateProjectAsync(project);
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// 删除指定协议项目。
    /// </summary>
    /// <param name="id">协议项目 ID。</param>
    /// <returns>项目列表页重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _repository.DeleteProjectAsync(id);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// 从详情页删除协议项目，并根据当前上下文选择后续跳转页面。
    /// </summary>
    /// <param name="id">要删除的协议项目 ID。</param>
    /// <param name="currentProjectId">当前详情页所在项目 ID。</param>
    /// <param name="tab">删除前所在页签。</param>
    /// <returns>合适页面的重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFromDetails(int id, int currentProjectId, string tab = "solution")
    {
        await _repository.DeleteProjectAsync(id);
        if (id == currentProjectId)
        {
            // 删除当前项目后，优先进入剩余项目；没有项目时进入创建页，避免停留在失效详情页。
            var nextProject = (await _repository.GetProjectsAsync()).FirstOrDefault();
            if (nextProject == null)
            {
                return RedirectToAction(nameof(Create));
            }

            return RedirectToTab(nextProject.Id, "solution");
        }

        return RedirectToTab(currentProjectId, tab);
    }

    /// <summary>
    /// 显示项目详情工作台。
    /// </summary>
    /// <param name="id">协议项目 ID。</param>
    /// <param name="classId">可选的当前选中通讯类 ID。</param>
    /// <param name="enumId">可选的当前选中枚举 ID。</param>
    /// <returns>项目详情视图。</returns>
    public async Task<IActionResult> Details(int id, int? classId = null, int? enumId = null)
    {
        var model = await _repository.GetWorkspaceAsync(id);
        if (model == null)
        {
            return NotFound();
        }
        // 如果 URL 中指定的选中项不存在，则回落到当前项目中的第一项，保证页面有稳定默认焦点。
        model.SelectedClassId = classId.HasValue && model.Classes.Any(c => c.Id == classId.Value)
            ? classId.Value
            : model.Classes.FirstOrDefault()?.Id;
        model.SelectedEnumId = enumId.HasValue && model.Enums.Any(e => e.Id == enumId.Value)
            ? enumId.Value
            : model.Enums.FirstOrDefault()?.Id;
        return View(model);
    }

    /// <summary>
    /// 为项目新增一个通讯类。
    /// </summary>
    /// <param name="entity">通讯类实体。</param>
    /// <returns>通讯类页签重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddClass(CommClassEntity entity)
    {
        await _repository.AddClassAsync(entity);
        return RedirectToTab(entity.ProjectId, "classes");
    }

    /// <summary>
    /// 删除指定通讯类及其字段。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="classId">通讯类 ID。</param>
    /// <returns>通讯类页签重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteClass(int projectId, int classId)
    {
        await _repository.DeleteClassAsync(projectId, classId);
        return RedirectToTab(projectId, "classes");
    }

    /// <summary>
    /// 从表单读取字段配置并新增字段。
    /// </summary>
    /// <returns>字段区域重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddField()
    {
        var entity = ReadFieldFromForm();

        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            // 字段名称为空时不进入仓储层，直接回到当前字段列表并显示诊断信息。
            TempData["Diagnostics"] = "字段名不能为空。";
            return RedirectToFields(entity.ProjectId, entity.ClassId);
        }

        await _repository.AddFieldAsync(entity);
        return RedirectToFields(entity.ProjectId, entity.ClassId);
    }

    /// <summary>
    /// 从表单读取字段配置并更新字段。
    /// </summary>
    /// <returns>字段区域重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateField()
    {
        var entity = ReadFieldFromForm();
        entity.Id = ReadFormInt("Id");

        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            TempData["Diagnostics"] = "字段名不能为空。";
            return RedirectToFields(entity.ProjectId, entity.ClassId);
        }

        await _repository.UpdateFieldAsync(entity);
        return RedirectToFields(entity.ProjectId, entity.ClassId);
    }

    /// <summary>
    /// 删除指定通讯字段。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="classId">通讯类 ID。</param>
    /// <param name="fieldId">字段 ID。</param>
    /// <returns>字段区域重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteField(int projectId, int classId, int fieldId)
    {
        await _repository.DeleteFieldAsync(projectId, fieldId);
        return RedirectToFields(projectId, classId);
    }

    /// <summary>
    /// 调整指定字段在通讯类中的顺序。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="classId">通讯类 ID。</param>
    /// <param name="fieldId">字段 ID。</param>
    /// <param name="direction">移动方向，负数表示上移，正数表示下移。</param>
    /// <returns>字段区域重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveField(int projectId, int classId, int fieldId, int direction)
    {
        await _repository.MoveFieldAsync(projectId, fieldId, direction);
        return RedirectToFields(projectId, classId);
    }

    /// <summary>
    /// 新增枚举定义。
    /// </summary>
    /// <param name="entity">枚举定义实体。</param>
    /// <param name="classId">可选的当前通讯类 ID，用于保持页面选择状态。</param>
    /// <returns>枚举页签重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEnum(EnumDefinitionEntity entity, int? classId = null)
    {
        var enumId = await _repository.AddEnumAsync(entity);
        return RedirectToEnums(entity.ProjectId, classId, enumId);
    }

    /// <summary>
    /// 更新枚举定义。
    /// </summary>
    /// <param name="entity">枚举定义实体。</param>
    /// <param name="classId">可选的当前通讯类 ID，用于保持页面选择状态。</param>
    /// <returns>枚举页签重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEnum(EnumDefinitionEntity entity, int? classId = null)
    {
        await _repository.UpdateEnumAsync(entity);
        return RedirectToEnums(entity.ProjectId, classId, entity.Id);
    }

    /// <summary>
    /// 新增枚举成员。
    /// </summary>
    /// <param name="entity">枚举成员实体。</param>
    /// <param name="classId">可选的当前通讯类 ID，用于保持页面选择状态。</param>
    /// <returns>枚举页签重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEnumMember(EnumMemberEntity entity, int? classId = null)
    {
        await _repository.AddEnumMemberAsync(entity);
        return RedirectToEnums(entity.ProjectId, classId, entity.EnumId);
    }

    /// <summary>
    /// 更新枚举成员。
    /// </summary>
    /// <param name="entity">枚举成员实体。</param>
    /// <param name="classId">可选的当前通讯类 ID，用于保持页面选择状态。</param>
    /// <returns>枚举页签重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEnumMember(EnumMemberEntity entity, int? classId = null)
    {
        await _repository.UpdateEnumMemberAsync(entity);
        return RedirectToEnums(entity.ProjectId, classId, entity.EnumId);
    }

    /// <summary>
    /// 删除指定枚举及其枚举成员。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="enumId">枚举 ID。</param>
    /// <param name="classId">可选的当前通讯类 ID，用于保持页面选择状态。</param>
    /// <returns>枚举页签重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEnum(int projectId, int enumId, int? classId = null)
    {
        await _repository.DeleteEnumAsync(projectId, enumId);
        return RedirectToEnums(projectId, classId);
    }

    /// <summary>
    /// 删除指定枚举成员。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="memberId">枚举成员 ID。</param>
    /// <param name="classId">可选的当前通讯类 ID，用于保持页面选择状态。</param>
    /// <returns>枚举页签重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEnumMember(int projectId, int memberId, int? classId = null)
    {
        await _repository.DeleteEnumMemberAsync(projectId, memberId);
        return RedirectToEnums(projectId, classId);
    }

    /// <summary>
    /// 新增自定义字段校验方法。
    /// </summary>
    /// <param name="entity">校验方法实体。</param>
    /// <returns>项目详情页重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddValidationMethod(ValidationMethodEntity entity)
    {
        await _repository.AddValidationMethodAsync(entity);
        return RedirectToAction(nameof(Details), new { id = entity.ProjectId });
    }

    /// <summary>
    /// 删除自定义字段校验方法。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="methodId">校验方法 ID。</param>
    /// <returns>项目详情页重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteValidationMethod(int projectId, int methodId)
    {
        await _repository.DeleteValidationMethodAsync(projectId, methodId);
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    /// <summary>
    /// 预览完整生成项目的源码文件。
    /// </summary>
    /// <param name="id">协议项目 ID。</param>
    /// <returns>源码预览视图。</returns>
    public async Task<IActionResult> Preview(int id)
    {
        var schema = await _repository.BuildSchemaAsync(id);
        if (schema == null)
        {
            return NotFound();
        }

        // 预览模式只生成内存文件和 ZIP，不写入本地目录。
        var result = await _generator.GenerateAsync(new GenerateRequest
        {
            Schema = schema,
            OutputMode = GenerateOutputMode.Preview
        });

        return View(result);
    }

    /// <summary>
    /// 预览当前通讯类生成出的单个源码文件。
    /// </summary>
    /// <param name="id">协议项目 ID。</param>
    /// <param name="classId">通讯类 ID。</param>
    /// <returns>源码预览视图。</returns>
    public async Task<IActionResult> PreviewClass(int id, int classId)
    {
        var workspace = await _repository.GetWorkspaceAsync(id);
        if (workspace == null)
        {
            return NotFound();
        }

        var classEntity = workspace.Classes.FirstOrDefault(c => c.Id == classId);
        if (classEntity == null)
        {
            return NotFound();
        }

        var schema = await _repository.BuildSchemaAsync(id);
        if (schema == null)
        {
            return NotFound();
        }

        var result = await _generator.GenerateClassAsync(schema, classEntity.Name);
        ViewData["Title"] = "当前类源码预览";
        return View("Preview", result);
    }

    /// <summary>
    /// 预览当前枚举生成出的单个源码文件。
    /// </summary>
    /// <param name="id">协议项目 ID。</param>
    /// <param name="enumId">枚举 ID。</param>
    /// <returns>源码预览视图。</returns>
    public async Task<IActionResult> PreviewEnum(int id, int enumId)
    {
        var workspace = await _repository.GetWorkspaceAsync(id);
        if (workspace == null)
        {
            return NotFound();
        }

        var enumEntity = workspace.Enums.FirstOrDefault(e => e.Id == enumId);
        if (enumEntity == null)
        {
            return NotFound();
        }

        var schema = await _repository.BuildSchemaAsync(id);
        if (schema == null)
        {
            return NotFound();
        }

        var result = await _generator.GenerateEnumAsync(schema, enumEntity.Name);
        ViewData["Title"] = "当前枚举源码预览";
        return View("Preview", result);
    }

    /// <summary>
    /// 下载完整生成项目的 ZIP 包。
    /// </summary>
    /// <param name="id">协议项目 ID。</param>
    /// <returns>ZIP 文件下载结果或预览页重定向结果。</returns>
    public async Task<IActionResult> Download(int id)
    {
        var schema = await _repository.BuildSchemaAsync(id);
        if (schema == null)
        {
            return NotFound();
        }

        var result = await _generator.GenerateAsync(new GenerateRequest
        {
            Schema = schema,
            OutputMode = GenerateOutputMode.DownloadZip
        });

        if (!result.Success)
        {
            // 下载前仍复用生成器校验结果，失败时返回预览页展示诊断，避免下载无效文件。
            TempData["Diagnostics"] = string.Join(Environment.NewLine, result.Diagnostics);
            return RedirectToAction(nameof(Preview), new { id });
        }

        return File(result.ZipBytes, "application/zip", schema.ProjectName + ".zip");
    }

    /// <summary>
    /// 将完整生成项目写入本地 artifacts 目录。
    /// </summary>
    /// <param name="id">协议项目 ID。</param>
    /// <returns>源码预览页重定向结果。</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WriteLocal(int id)
    {
        var schema = await _repository.BuildSchemaAsync(id);
        if (schema == null)
        {
            return NotFound();
        }

        var result = await _generator.GenerateAsync(new GenerateRequest
        {
            Schema = schema,
            OutputMode = GenerateOutputMode.WriteLocal,
            OutputRootDirectory = _paths.GeneratedRootDirectory
        });

        // 写入结果通过 TempData 带回预览页，便于用户确认实际输出目录或查看失败原因。
        TempData["Diagnostics"] = result.Success
            ? "已写入：" + result.LocalOutputDirectory
            : string.Join(Environment.NewLine, result.Diagnostics);

        return RedirectToAction(nameof(Preview), new { id });
    }

    /// <summary>
    /// 从当前请求表单读取字符串值。
    /// </summary>
    /// <param name="key">表单字段名。</param>
    /// <param name="defaultValue">空值时使用的默认值。</param>
    /// <returns>整理后的表单字符串。</returns>
    private string ReadFormString(string key, string defaultValue = "")
    {
        var value = Request.Form[key].ToString();
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    /// <summary>
    /// 从当前请求表单读取整数值。
    /// </summary>
    /// <param name="key">表单字段名。</param>
    /// <param name="defaultValue">解析失败时使用的默认值。</param>
    /// <returns>解析出的整数值。</returns>
    private int ReadFormInt(string key, int defaultValue = 0)
    {
        var value = Request.Form[key].ToString();
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    /// <summary>
    /// 从当前请求表单组装通讯字段实体。
    /// </summary>
    /// <returns>通讯字段实体。</returns>
    private CommFieldEntity ReadFieldFromForm()
    {
        // 字段表单字段较多，集中读取可以保持新增和更新字段时的默认值一致。
        return new CommFieldEntity
        {
            ProjectId = ReadFormInt("ProjectId"),
            ClassId = ReadFormInt("ClassId"),
            Name = ReadFormString("Name"),
            TypeKind = ReadFormString("TypeKind", "Basic"),
            TypeName = ReadFormString("TypeName", "byte"),
            CollectionKind = ReadFormString("CollectionKind", "None"),
            OrderIndex = ReadFormString("OrderIndex", "0"),
            StartIndex = ReadFormInt("StartIndex", -1),
            ArrayLength = ReadFormString("ArrayLength", "-1"),
            AutoLengthType = string.Empty,
            EnumEndType = ReadFormString("EnumEndType"),
            EndianType = ReadFormString("EndianType", "Big_ABCD"),
            Desc = ReadFormString("Desc"),
            Remarks = ReadFormString("Remarks"),
            ValidationMethodName = ReadFormString("ValidationMethodName")
        };
    }

    /// <summary>
    /// 重定向到项目详情页的字段区域。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="classId">通讯类 ID。</param>
    /// <returns>字段区域重定向结果。</returns>
    private IActionResult RedirectToFields(int projectId, int classId)
    {
        return Redirect((Url.Action(nameof(Details), new { id = projectId, classId }) ?? $"/Projects/Details/{projectId}?classId={classId}") + "#fields");
    }

    /// <summary>
    /// 重定向到项目详情页的枚举区域。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="classId">可选的通讯类 ID。</param>
    /// <param name="enumId">可选的枚举 ID。</param>
    /// <returns>枚举区域重定向结果。</returns>
    private IActionResult RedirectToEnums(int projectId, int? classId = null, int? enumId = null)
    {
        return RedirectToTab(projectId, "enums", classId, enumId);
    }

    /// <summary>
    /// 重定向到项目详情页的指定页签。
    /// </summary>
    /// <param name="projectId">协议项目 ID。</param>
    /// <param name="tab">页签锚点名称。</param>
    /// <param name="classId">可选的通讯类 ID。</param>
    /// <param name="enumId">可选的枚举 ID。</param>
    /// <returns>指定页签重定向结果。</returns>
    private IActionResult RedirectToTab(int projectId, string tab, int? classId = null, int? enumId = null)
    {
        var cleanTab = string.IsNullOrWhiteSpace(tab) ? "solution" : tab.Trim().TrimStart('#');
        return Redirect((Url.Action(nameof(Details), new { id = projectId, classId, enumId }) ?? $"/Projects/Details/{projectId}") + "#" + cleanTab);
    }
}
