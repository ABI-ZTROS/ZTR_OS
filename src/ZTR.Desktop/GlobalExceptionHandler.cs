using System.Windows;
using System.Windows.Threading;
using Serilog;

namespace ZTR.Desktop;

/// <summary>
/// 三层异常处理：
/// 1. AppDomain.CurrentDomain.UnhandledException（非UI线程）
/// 2. TaskScheduler.UnobservedTaskException（Task 未观察）
/// 3. Dispatcher.DispatcherUnhandledException（UI 线程）
/// </summary>
public static class GlobalExceptionHandler
{
    /// <summary>
    /// 注册全局异常处理器
    /// </summary>
    public static void Setup()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            ForceLog.Write($"[FATAL] AppDomain.UnhandledException  IsTerminating={e.IsTerminating}");
            ForceLog.Write(ex?.ToString() ?? "(ExceptionObject 不是 Exception 类型)");
            try { Log.Fatal(ex, "[FATAL] 非UI线程致命异常 AppDomain.UnhandledException (终止={IsTerminating})", e.IsTerminating); } catch { }
            try { ForceCrashDump.WriteForceCrashDump(ex); } catch { }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            ForceLog.Write($"[WARN] TaskScheduler.UnobservedTaskException: {e.Exception}");
            try { Log.Error(e.Exception, "[WARN] Task 未观察异常 UnobservedTaskException"); } catch { }
            e.SetObserved();
        };

        Dispatcher.DispatcherUnhandledException += (_, e) =>
        {
            ForceLog.Write($"[FATAL] DispatcherUnhandledException: {e.Exception}");
            try { Log.Fatal(e.Exception, "[FATAL] UI 线程未处理异常 DispatcherUnhandledException"); } catch { }
            try { ForceCrashDump.WriteForceCrashDump(e.Exception); } catch { }
            try { ShowCrashReport(e.Exception); e.Handled = true; }
            catch { e.Handled = true; }
        };

        ForceLog.Write("[EX] 全局异常处理器已注册（三层防护）");
    }

    /// <summary>
    /// 显示崩溃报告
    /// </summary>
    public static void ShowCrashReport(Exception ex)
    {
        string? forceCrashPath = null;
        try
        {
            forceCrashPath = ForceCrashDump.WriteForceCrashDump(ex);
        }
        catch { }

        try
        {
            var msg = $"[FATAL] 哎呀，程序出了点问题！\n\n" +
                      $"错误信息：{ex.Message}\n\n" +
                      $"强制崩溃转储: {forceCrashPath ?? "(未写入)"}\n" +
                      $"强制死日志: {ForceLog.GetLogPath()}\n\n" +
                      $"你可以把这些文件发给开发者排查问题。";

            MessageBox.Show(msg, "ZTR_OS 崩溃了",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception reportEx)
        {
            ForceLog.Write($"[FATAL] ShowCrashReport 内部也崩了: {reportEx.Message}");
            try
            {
                MessageBox.Show(
                    $"崩溃！\n原始错误：{ex.Message}\n崩溃报告也崩了：{reportEx.Message}\n" +
                    $"强制转储：{forceCrashPath ?? "(无)"}\n死日志：{ForceLog.GetLogPath()}",
                    "ZTR_OS 双重崩溃",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);
            }
            catch { }
        }
    }
}
