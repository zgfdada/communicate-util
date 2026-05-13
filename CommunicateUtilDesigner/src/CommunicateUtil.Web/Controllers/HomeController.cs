using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CommunicateUtil.Web.Models;

namespace CommunicateUtil.Web.Controllers;

/// <summary>
/// 提供站点首页跳转、隐私页和错误页等基础页面入口。
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    /// <summary>
    /// 初始化基础页面控制器。
    /// </summary>
    /// <param name="logger">控制器日志记录器。</param>
    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 将默认首页请求重定向到项目设计器列表。
    /// </summary>
    /// <returns>项目列表页重定向结果。</returns>
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Projects");
    }

    /// <summary>
    /// 显示隐私页面。
    /// </summary>
    /// <returns>隐私页面视图。</returns>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// 显示错误页面，并附带当前请求标识。
    /// </summary>
    /// <returns>错误页面视图。</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
