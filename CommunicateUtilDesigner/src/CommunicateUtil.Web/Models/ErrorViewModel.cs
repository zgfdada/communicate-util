namespace CommunicateUtil.Web.Models;

/// <summary>
/// 表示错误页面展示所需的请求诊断信息。
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// 当前请求的跟踪标识。
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// 获取是否需要在错误页显示请求标识。
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
