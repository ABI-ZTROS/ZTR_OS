using System.IO;
using System.Windows;

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
    /// <returns>(ok, message) 元组：ok=true 表示已安装</returns>
    public static (bool ok, string message) VerifyEnvironment()
    {
        try
        {
            // 用 CefSharp 风格：检查 WebView2 Runtime 是否存在
            // 通过检查注册表或文件系统来判断
            var runtimePath = GetWebView2RuntimePath();
            if (!string.IsNullOrEmpty(runtimePath))
            {
                ForceLog.Write($"[BOOT] WebView2 Runtime 已找到: {runtimePath}");
                return (true, $"WebView2 Runtime 已就绪");
            }

            ForceLog.Write("[BOOT] WebView2 Runtime 未找到");
            return (false, "WebView2 Runtime 未安装，请安装 Microsoft Edge WebView2 运行时");
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[BOOT] WebView2 Runtime 检查异常: {ex.Message}");
            return (false, $"WebView2 Runtime 检查失败: {ex.Message}");
        }
    }

    private static string? GetWebView2RuntimePath()
    {
        // 检查 WebView2 Runtime 的标准安装路径
        string[] candidates =
        {
            @"C:\Program Files (x86)\Microsoft\EdgeWebView\Application\",
            @"C:\Program Files\Microsoft\EdgeWebView\Application\",
            @"C:\Program Files (x86)\Microsoft\WebView2\Application\",
        };

        foreach (var path in candidates)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    // 查找版本子目录
                    var dirs = Directory.GetDirectories(path);
                    if (dirs.Length > 0)
                        return dirs[^1];
                    return path;
                }
            }
            catch { }
        }

        // 尝试通过 CoreWebView2Environment 异步检查（如果可用）
        try
        {
            // Fallback: 检查是否有可用的 WebView2
            var envPath = Environment.GetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE");
            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                return envPath;
        }
        catch { }

        return null;
    }
}