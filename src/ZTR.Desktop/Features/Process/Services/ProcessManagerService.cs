using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZTR.Desktop.Features.Process.Services;

public class ProcessManagerService : IProcessManagerService
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool SetProcessAffinityMask(IntPtr hProcess, long dwProcessAffinityMask);

    [DllImport("kernel32.dll")]
    private static extern bool SetPriorityClass(IntPtr hProcess, int dwPriorityClass);

    [DllImport("kernel32.dll")]
    private static extern bool GetProcessAffinityMask(IntPtr hProcess, out long lpProcessAffinityMask, out long lpSystemAffinityMask);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    private const int PROCESS_QUERY_INFORMATION = 0x0400;
    private const int PROCESS_SET_INFORMATION = 0x0200;
    private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    public bool SetProcessAffinity(int processId, long affinityMask)
    {
        try
        {
            var handle = OpenProcess(PROCESS_SET_INFORMATION | PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (handle == IntPtr.Zero) return false;
            try
            {
                return SetProcessAffinityMask(handle, affinityMask);
            }
            finally
            {
                CloseHandle(handle);
            }
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[PROC] SetProcessAffinity 失败: {ex.Message}");
            return false;
        }
    }

    public bool SetProcessPriority(int processId, int priority)
    {
        try
        {
            var handle = OpenProcess(PROCESS_SET_INFORMATION, false, processId);
            if (handle == IntPtr.Zero) return false;
            try
            {
                return SetPriorityClass(handle, priority);
            }
            finally
            {
                CloseHandle(handle);
            }
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[PROC] SetProcessPriority 失败: {ex.Message}");
            return false;
        }
    }

    public long GetProcessAffinity(int processId)
    {
        try
        {
            var handle = OpenProcess(PROCESS_QUERY_INFORMATION, false, processId);
            if (handle == IntPtr.Zero) return 0;
            try
            {
                if (GetProcessAffinityMask(handle, out var mask, out _))
                    return mask;
                return 0;
            }
            finally
            {
                CloseHandle(handle);
            }
        }
        catch { return 0; }
    }

    public int GetProcessPriority(int processId)
    {
        try
        {
            using var proc = Process.GetProcessById(processId);
            return (int)proc.PriorityClass;
        }
        catch { return 0; }
    }
}
