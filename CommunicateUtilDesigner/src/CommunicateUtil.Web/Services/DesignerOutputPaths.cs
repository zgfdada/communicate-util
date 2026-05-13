namespace CommunicateUtil.Web.Services;

/// <summary>
/// 统一管理设计器运行时使用的本地输出路径。
/// </summary>
public sealed class DesignerOutputPaths
{
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// 初始化设计器路径服务。
    /// </summary>
    /// <param name="environment">Web 主机环境。</param>
    public DesignerOutputPaths(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// 获取设计器应用数据目录，不存在时自动创建。
    /// </summary>
    public string AppDataDirectory
    {
        get
        {
            // App_Data 放在 Web 项目内容根目录下，便于本地运行和部署时定位。
            var path = Path.Combine(_environment.ContentRootPath, "App_Data");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    /// <summary>
    /// 获取设计器 SQLite 数据库文件路径。
    /// </summary>
    public string DatabasePath => Path.Combine(AppDataDirectory, "designer.db");

    /// <summary>
    /// 获取生成项目源码的本地根目录，不存在时自动创建。
    /// </summary>
    public string GeneratedRootDirectory
    {
        get
        {
            // 从 Web 项目目录回到仓库根目录，再统一写入 CommunicateUtilDesigner/artifacts/generated。
            var repoRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", ".."));
            var path = Path.Combine(repoRoot, "CommunicateUtilDesigner", "artifacts", "generated");
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
