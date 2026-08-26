using System.IO;

namespace ZTR.Desktop;

/// <summary>
/// 完全不依赖任何第三方库的死日志机制。
/// 三重输出：Console.Error + Debug.WriteLine + 直接写文件。
/// 进程只要活着，这玩意基本都能写出去。
/// </summary>
public static class ForceLog
{
    private static readonly string LogPath = Path.Combine(
        AppContext.BaseDirectory, "logs",
        $"force-boot-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    static ForceLog()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!); } catch { }
    }

    /// <summary>
    /// 写入强制死日志
    /// </summary>
    public static void Write(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
        try { Console.Error.Write(line); } catch { }
        try { System.Diagnostics.Debug.Write(line); } catch { }
        try { File.AppendAllText(LogPath, line); } catch { }
    }

    /// <summary>
    /// 获取日志文件路径
    /// </summary>
    public static string GetLogPath() => LogPath;
}
