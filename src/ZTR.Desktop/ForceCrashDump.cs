using System.IO;

namespace ZTR.Desktop;

/// <summary>
/// 不依赖 Serilog 的崩溃转储。
/// 在 Serilog 初始化之前也能使用。
/// </summary>
public static class ForceCrashDump
{
    /// <summary>
    /// 写入强制崩溃转储
    /// </summary>
    /// <param name="ex">异常对象</param>
    /// <returns>转储文件路径</returns>
    public static string WriteForceCrashDump(Exception? ex)
    {
        try
        {
            var crashDir = Path.Combine(AppContext.BaseDirectory, "logs", "crashes");
            Directory.CreateDirectory(crashDir);
            var fileName = $"force-crash-{DateTime.Now:yyyyMMdd-HHmmss-fff}.log";
            var filePath = Path.Combine(crashDir, fileName);

            var dump =
                $"=== ZTR_OS 强制崩溃转储（Serilog 可能未初始化） ==={Environment.NewLine}" +
                $"时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}{Environment.NewLine}" +
                $"进程：{Environment.ProcessId}{Environment.NewLine}" +
                $"OS：{Environment.OSVersion}{Environment.NewLine}" +
                $"{Environment.NewLine}--- 异常信息 ---{Environment.NewLine}{ex}{Environment.NewLine}" +
                $"{Environment.NewLine}--- 内部异常 ---{Environment.NewLine}{ex?.InnerException}{Environment.NewLine}";

            File.WriteAllText(filePath, dump);
            ForceLog.Write($"[DUMP] 强制崩溃转储已写入: {filePath}");
            return filePath;
        }
        catch (Exception wtf)
        {
            ForceLog.Write($"连强制崩溃转储都写不进去了: {wtf.Message}");
            return "(写入失败)";
        }
    }
}
