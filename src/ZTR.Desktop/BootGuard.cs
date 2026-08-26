using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace ZTR.Desktop;

/// <summary>
/// 启动防护：配置早期启动模式、校验运行环境依赖。
/// </summary>
public static class BootGuard
{
    /// <summary>
    /// 设置 ShutdownMode = OnExplicitShutdown，防止启动窗口意外关闭导致静默退出。
    /// 必须在显示任何窗口之前调用。
    /// </summary>
    public static void ConfigureEarlyStartup()
    {
        try
        {
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ForceLog.Write("[BOOT] ShutdownMode = OnExplicitShutdown（防静默退出）");
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[BOOT] 设置 ShutdownMode 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查 WebView2 Runtime 是否已安装。
    /// </summary>
    /// <returns>(ok, message) 元组：ok=true 表示已安装，message 为版本信息或错误描述</returns>
    public static async Task<(bool ok, string message)> VerifyEnvironment()
    {
        try
        {
            var version = await CoreWebView2Environment.GetAvailableBrowserVersionAsync();
            if (!string.IsNullOrEmpty(version))
            {
                ForceLog.Write($"[BOOT] WebView2 Runtime 已安装: {version}");
                return (true, $"WebView2 Runtime {version}");
            }

            ForceLog.Write("[BOOT] WebView2 Runtime 未安装");
            return (false, "WebView2 Runtime 未安装，请安装 Microsoft Edge WebView2 运行时");
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[BOOT] WebView2 Runtime 检查异常: {ex.Message}");
            return (false, $"WebView2 Runtime 检查失败: {ex.Message}");
        }
    }
}