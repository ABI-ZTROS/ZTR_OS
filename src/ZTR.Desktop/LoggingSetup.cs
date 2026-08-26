using System.IO;
using Serilog;

namespace ZTR.Desktop;

/// <summary>
/// Serilog 精简配置。
/// 主日志：Warning+，5MB 滚动保留 5 份。
/// 调试日志：Debug+，2MB 滚动保留 3 份。
/// </summary>
public static class LoggingSetup
{
    /// <summary>
    /// 配置 Serilog 日志系统
    /// </summary>
    public static void Configure()
    {
        try
        {
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);

            string mainLogPath = Path.Combine(logDir, "ztr-os-.log");
            string debugLogPath = Path.Combine(logDir, "debug-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .MinimumLevel.Override("ZTR_OS", Serilog.Events.LogEventLevel.Information)
                .MinimumLevel.Override("ZTR_OS.Features.Hardware", Serilog.Events.LogEventLevel.Error)
                .MinimumLevel.Override("ZTR_OS.Features.Sensor", Serilog.Events.LogEventLevel.Error)
                .MinimumLevel.Override("ZTR_OS.Features.PerformanceModes", Serilog.Events.LogEventLevel.Error)
                .WriteTo.File(
                    path: mainLogPath,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 5 * 1024 * 1024,
                    retainedFileCountLimit: 5,
                    shared: false,
                    outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.Logger(lc => lc
                    .MinimumLevel.Debug()
                    .WriteTo.File(
                        path: debugLogPath,
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: 2 * 1024 * 1024,
                        retainedFileCountLimit: 3,
                        outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"))
                .CreateLogger();

            ForceLog.Write($"[BOOT-LOG] Serilog 已精简：主日志 Warning+ (5MB×5份) + 调试日志 Debug+ (2MB×3份)");
            ForceLog.Write($"[BOOT-LOG] 主日志: {mainLogPath}");
            ForceLog.Write($"[BOOT-LOG] 调试日志: {debugLogPath}");
        }
        catch (Exception serilogEx)
        {
            ForceLog.Write($"[BOOT-LOG] Serilog 初始化失败: {serilogEx.Message}");
            try
            {
                var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
                string fallbackPath = Path.Combine(logDir, "ztr-os-fallback-.log");
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Warning()
                    .WriteTo.File(fallbackPath, rollOnFileSizeLimit: true, fileSizeLimitBytes: 5 * 1024 * 1024, retainedFileCountLimit: 5)
                    .CreateLogger();
                ForceLog.Write("[BOOT-LOG] 已用兜底配置初始化 Serilog");
            }
            catch { }
        }
    }

    /// <summary>
    /// 清理 7 天前的旧日志
    /// </summary>
    public static void CleanupOldLogs()
    {
        try
        {
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            if (!Directory.Exists(logDir)) return;

            var oldFiles = Directory.GetFiles(logDir, "ztr-os-*.log")
                .Concat(Directory.GetFiles(logDir, "debug-*.log"))
                .Concat(Directory.GetFiles(logDir, "ztr-os-fallback-*.log"))
                .Select(f => new FileInfo(f))
                .Where(f => (DateTime.Now - f.CreationTime).TotalDays > 7)
                .ToList();

            foreach (var file in oldFiles)
            {
                try { file.Delete(); }
                catch { }
            }

            if (oldFiles.Count > 0)
            {
                ForceLog.Write($"[CLEAN] 已清理 {oldFiles.Count} 个旧日志文件");
                try { Log.Information("[CLEAN] 已清理 {Count} 个旧日志文件", oldFiles.Count); } catch { }
            }
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[CLEAN] 清理旧日志失败: {ex.Message}");
            try { Log.Warning(ex, "清理旧日志文件失败"); } catch { }
        }
    }
}
