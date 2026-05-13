using CommunicateUtil.Generator;
using CommunicateUtil.Generator.Models;
using CommunicateUtil.Web.Data;
using CommunicateUtil.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommunicateUtil.Web.Controllers;

public class ProjectsController : Controller
{
    private readonly IDesignerRepository _repository;
    private readonly ICommunicationCodeGenerator _generator;
    private readonly DesignerOutputPaths _paths;

    public ProjectsController(
        IDesignerRepository repository,
        ICommunicationCodeGenerator generator,
        DesignerOutputPaths paths)
    {
        _repository = repository;
        _generator = generator;
        _paths = paths;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _repository.GetProjectsAsync());
    }

    public IActionResult Create()
    {
        return View(new ProtocolProjectEntity());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProtocolProjectEntity project)
    {
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _repository.DeleteProjectAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFromDetails(int id, int currentProjectId, string tab = "solution")
    {
        await _repository.DeleteProjectAsync(id);
        if (id == currentProjectId)
        {
            var nextProject = (await _repository.GetProjectsAsync()).FirstOrDefault();
            if (nextProject == null)
            {
                return RedirectToAction(nameof(Create));
            }

            return RedirectToTab(nextProject.Id, "solution");
        }

        return RedirectToTab(currentProjectId, tab);
    }

    public async Task<IActionResult> Details(int id, int? classId = null, int? enumId = null)
    {
        var model = await _repository.GetWorkspaceAsync(id);
        if (model == null)
        {
            return NotFound();
        }
        model.SelectedClassId = classId.HasValue && model.Classes.Any(c => c.Id == classId.Value)
            ? classId.Value
            : model.Classes.FirstOrDefault()?.Id;
        model.SelectedEnumId = enumId.HasValue && model.Enums.Any(e => e.Id == enumId.Value)
            ? enumId.Value
            : model.Enums.FirstOrDefault()?.Id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddClass(CommClassEntity entity)
    {
        await _repository.AddClassAsync(entity);
        return RedirectToTab(entity.ProjectId, "classes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteClass(int projectId, int classId)
    {
        await _repository.DeleteClassAsync(projectId, classId);
        return RedirectToTab(projectId, "classes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddField()
    {
        var entity = ReadFieldFromForm();

        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            TempData["Diagnostics"] = "字段名不能为空。";
            return RedirectToFields(entity.ProjectId, entity.ClassId);
        }

        await _repository.AddFieldAsync(entity);
        return RedirectToFields(entity.ProjectId, entity.ClassId);
    }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteField(int projectId, int classId, int fieldId)
    {
        await _repository.DeleteFieldAsync(projectId, fieldId);
        return RedirectToFields(projectId, classId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveField(int projectId, int classId, int fieldId, int direction)
    {
        await _repository.MoveFieldAsync(projectId, fieldId, direction);
        return RedirectToFields(projectId, classId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEnum(EnumDefinitionEntity entity, int? classId = null)
    {
        var enumId = await _repository.AddEnumAsync(entity);
        return RedirectToEnums(entity.ProjectId, classId, enumId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEnum(EnumDefinitionEntity entity, int? classId = null)
    {
        await _repository.UpdateEnumAsync(entity);
        return RedirectToEnums(entity.ProjectId, classId, entity.Id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEnumMember(EnumMemberEntity entity, int? classId = null)
    {
        await _repository.AddEnumMemberAsync(entity);
        return RedirectToEnums(entity.ProjectId, classId, entity.EnumId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEnumMember(EnumMemberEntity entity, int? classId = null)
    {
        await _repository.UpdateEnumMemberAsync(entity);
        return RedirectToEnums(entity.ProjectId, classId, entity.EnumId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEnum(int projectId, int enumId, int? classId = null)
    {
        await _repository.DeleteEnumAsync(projectId, enumId);
        return RedirectToEnums(projectId, classId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEnumMember(int projectId, int memberId, int? classId = null)
    {
        await _repository.DeleteEnumMemberAsync(projectId, memberId);
        return RedirectToEnums(projectId, classId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddValidationMethod(ValidationMethodEntity entity)
    {
        await _repository.AddValidationMethodAsync(entity);
        return RedirectToAction(nameof(Details), new { id = entity.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteValidationMethod(int projectId, int methodId)
    {
        await _repository.DeleteValidationMethodAsync(projectId, methodId);
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    public async Task<IActionResult> Preview(int id)
    {
        var schema = await _repository.BuildSchemaAsync(id);
        if (schema == null)
        {
            return NotFound();
        }

        var result = await _generator.GenerateAsync(new GenerateRequest
        {
            Schema = schema,
            OutputMode = GenerateOutputMode.Preview
        });

        return View(result);
    }

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
            TempData["Diagnostics"] = string.Join(Environment.NewLine, result.Diagnostics);
            return RedirectToAction(nameof(Preview), new { id });
        }

        return File(result.ZipBytes, "application/zip", schema.ProjectName + ".zip");
    }

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

        TempData["Diagnostics"] = result.Success
            ? "已写入：" + result.LocalOutputDirectory
            : string.Join(Environment.NewLine, result.Diagnostics);

        return RedirectToAction(nameof(Preview), new { id });
    }

    private string ReadFormString(string key, string defaultValue = "")
    {
        var value = Request.Form[key].ToString();
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private int ReadFormInt(string key, int defaultValue = 0)
    {
        var value = Request.Form[key].ToString();
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private CommFieldEntity ReadFieldFromForm()
    {
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

    private IActionResult RedirectToFields(int projectId, int classId)
    {
        return Redirect((Url.Action(nameof(Details), new { id = projectId, classId }) ?? $"/Projects/Details/{projectId}?classId={classId}") + "#fields");
    }

    private IActionResult RedirectToEnums(int projectId, int? classId = null, int? enumId = null)
    {
        return RedirectToTab(projectId, "enums", classId, enumId);
    }

    private IActionResult RedirectToTab(int projectId, string tab, int? classId = null, int? enumId = null)
    {
        var cleanTab = string.IsNullOrWhiteSpace(tab) ? "solution" : tab.Trim().TrimStart('#');
        return Redirect((Url.Action(nameof(Details), new { id = projectId, classId, enumId }) ?? $"/Projects/Details/{projectId}") + "#" + cleanTab);
    }
}
