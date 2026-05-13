namespace CommunicateUtil.Web.Services;

public sealed class DesignerOutputPaths
{
    private readonly IWebHostEnvironment _environment;

    public DesignerOutputPaths(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string AppDataDirectory
    {
        get
        {
            var path = Path.Combine(_environment.ContentRootPath, "App_Data");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public string DatabasePath => Path.Combine(AppDataDirectory, "designer.db");

    public string GeneratedRootDirectory
    {
        get
        {
            var repoRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", ".."));
            var path = Path.Combine(repoRoot, "CommunicateUtilDesigner", "artifacts", "generated");
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
