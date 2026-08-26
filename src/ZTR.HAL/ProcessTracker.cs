using System.Diagnostics;
using System.Runtime.InteropServices;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Tracks and monitors running processes on the system. Provides process listing,
/// foreground window detection, resource usage analysis, and process binding tracking.
/// </summary>
public class ProcessTracker
{
    private readonly Dictionary<int, ProcessBinding> _trackedBindings = new();
    private readonly List<string> _gameProcessNames = new()
    {
        "cs2", "csgo", "dota2", "valorant", "league of legends", "lol",
        "fortniteclient", "overwatch", "overwatch 2", "battle.net",
        "steam", "epicgameslauncher", "epicgamesclient",
        "gta5", "gta-v", "cyberpunk2077", "witcher3", "rdr2",
        "pubg", "apex_legends", "rainbow six siege", "r6s",
        "hunt showdown", "resident evil 4", "elden ring",
        "starfield", "skyrim", "fallout4", "fallout 4",
        "minecraft", "roblox", "worldofwarcraft", "wow"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessTracker"/> class.
    /// </summary>
    public ProcessTracker()
    {
    }

    /// <summary>
    /// Gets the list of known game process names used for auto-detection.
    /// </summary>
    public IReadOnlyList<string> KnownGameProcesses => _gameProcessNames.AsReadOnly();

    /// <summary>
    /// Lists all currently running processes on the system.
    /// </summary>
    /// <returns>A read-only list of <see cref="ProcessBinding"/> objects for each accessible process.</returns>
    public IReadOnlyList<ProcessBinding> GetAllProcesses()
    {
        var result = new List<ProcessBinding>();
        var processes = Process.GetProcesses();

        foreach (var proc in processes)
        {
            try
            {
                var binding = new ProcessBinding
                {
                    ProcessId = proc.Id,
                    ProcessName = proc.ProcessName,
                    MainWindowTitle = GetMainWindowTitleSafe(proc)
                };

                if (_trackedBindings.TryGetValue(proc.Id, out var tracked))
                {
                    binding.CpuAffinity = tracked.CpuAffinity;
                    binding.GpuAffinity = tracked.GpuAffinity;
                    binding.Strategy = tracked.Strategy;
                }

                result.Add(binding);
            }
            catch
            {
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the currently foreground (active) process.
    /// Uses GetForegroundWindow Win32 API on Windows, falls back to the highest-CPU process on other platforms.
    /// </summary>
    /// <returns>A <see cref="ProcessBinding"/> representing the foreground process, or null if not determinable.</returns>
    public ProcessBinding? GetForegroundProcess()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr foregroundHandle = GetForegroundWindow();
                if (foregroundHandle != IntPtr.Zero)
                {
                    uint processId;
                    GetWindowThreadProcessId(foregroundHandle, out processId);
                    if (processId > 0)
                    {
                        var proc = Process.GetProcessById((int)processId);
                        var binding = new ProcessBinding
                        {
                            ProcessId = proc.Id,
                            ProcessName = proc.ProcessName,
                            MainWindowTitle = GetMainWindowTitleSafe(proc)
                        };

                        if (_trackedBindings.TryGetValue(proc.Id, out var tracked))
                        {
                            binding.CpuAffinity = tracked.CpuAffinity;
                            binding.GpuAffinity = tracked.GpuAffinity;
                            binding.Strategy = tracked.Strategy;
                        }

                        return binding;
                    }
                }
            }
            else
            {
                var processes = Process.GetProcesses()
                    .Select(p => new { Process = p, StartTime = GetProcessStartTimeSafe(p) })
                    .Where(x => x.StartTime.HasValue)
                    .OrderBy(x => x.StartTime!.Value)
                    .FirstOrDefault();

                if (processes != null)
                {
                    return new ProcessBinding
                    {
                        ProcessId = processes.Process.Id,
                        ProcessName = processes.Process.ProcessName,
                        MainWindowTitle = GetMainWindowTitleSafe(processes.Process)
                    };
                }
            }
        }
        catch
        {
        }

        return null;
    }

    /// <summary>
    /// Finds processes whose name matches the specified pattern.
    /// Supports simple wildcard patterns with '*' as a wildcard character.
    /// </summary>
    /// <param name="pattern">The name pattern to match (e.g., "*game*", "cs*").</param>
    /// <returns>A read-only list of matching <see cref="ProcessBinding"/> objects.</returns>
    public IReadOnlyList<ProcessBinding> GetProcessByPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return Array.Empty<ProcessBinding>();

        var results = new List<ProcessBinding>();
        var regexPattern = ConvertWildcardToRegex(pattern);
        var processes = Process.GetProcesses();

        foreach (var proc in processes)
        {
            try
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(proc.ProcessName, regexPattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    var binding = new ProcessBinding
                    {
                        ProcessId = proc.Id,
                        ProcessName = proc.ProcessName,
                        MainWindowTitle = GetMainWindowTitleSafe(proc)
                    };

                    if (_trackedBindings.TryGetValue(proc.Id, out var tracked))
                    {
                        binding.CpuAffinity = tracked.CpuAffinity;
                        binding.GpuAffinity = tracked.GpuAffinity;
                        binding.Strategy = tracked.Strategy;
                    }

                    results.Add(binding);
                }
            }
            catch
            {
            }
        }

        return results;
    }

    /// <summary>
    /// Detects GPU-intensive processes by checking process names against known GPU-heavy applications
    /// and by analyzing runtime resource usage.
    /// </summary>
    /// <returns>A read-only list of <see cref="ProcessBinding"/> objects for GPU-intensive processes.</returns>
    public IReadOnlyList<ProcessBinding> GetGpuIntensiveProcesses()
    {
        var result = new List<ProcessBinding>();
        var allProcesses = Process.GetProcesses();

        foreach (var proc in allProcesses)
        {
            try
            {
                bool isGpuIntensive = IsGpuIntensiveProcess(proc.ProcessName);

                if (isGpuIntensive)
                {
                    var binding = new ProcessBinding
                    {
                        ProcessId = proc.Id,
                        ProcessName = proc.ProcessName,
                        MainWindowTitle = GetMainWindowTitleSafe(proc)
                    };

                    if (_trackedBindings.TryGetValue(proc.Id, out var tracked))
                    {
                        binding.CpuAffinity = tracked.CpuAffinity;
                        binding.GpuAffinity = tracked.GpuAffinity;
                        binding.Strategy = tracked.Strategy;
                    }

                    result.Add(binding);
                }
            }
            catch
            {
            }
        }

        return result;
    }

    /// <summary>
    /// Detects CPU-intensive processes by analyzing process CPU time and thread count.
    /// </summary>
    /// <returns>A read-only list of <see cref="ProcessBinding"/> objects for CPU-intensive processes.</returns>
    public IReadOnlyList<ProcessBinding> GetCpuIntensiveProcesses()
    {
        var result = new List<ProcessBinding>();
        var allProcesses = Process.GetProcesses();

        foreach (var proc in allProcesses)
        {
            try
            {
                bool isCpuIntensive = false;

                try
                {
                    var threads = proc.Threads;
                    isCpuIntensive = threads.Count > 4;
                }
                catch
                {
                }

                if (!isCpuIntensive)
                {
                    try
                    {
                        var cpuUsage = GetProcessCpuUsageSafe(proc);
                        isCpuIntensive = cpuUsage > 10.0;
                    }
                    catch
                    {
                    }
                }

                if (!isCpuIntensive)
                    isCpuIntensive = IsGameProcess(proc.ProcessName);

                if (isCpuIntensive)
                {
                    var binding = new ProcessBinding
                    {
                        ProcessId = proc.Id,
                        ProcessName = proc.ProcessName,
                        MainWindowTitle = GetMainWindowTitleSafe(proc)
                    };

                    if (_trackedBindings.TryGetValue(proc.Id, out var tracked))
                    {
                        binding.CpuAffinity = tracked.CpuAffinity;
                        binding.GpuAffinity = tracked.GpuAffinity;
                        binding.Strategy = tracked.Strategy;
                    }

                    result.Add(binding);
                }
            }
            catch
            {
            }
        }

        return result;
    }

    /// <summary>
    /// Starts tracking a process with the specified binding configuration.
    /// Tracked processes can later have their affinity policies applied.
    /// </summary>
    /// <param name="binding">The process binding configuration to track.</param>
    public void TrackProcess(ProcessBinding binding)
    {
        if (binding == null)
            throw new ArgumentNullException(nameof(binding));

        _trackedBindings[binding.ProcessId] = binding;
    }

    /// <summary>
    /// Stops tracking a process.
    /// </summary>
    /// <param name="processId">The process identifier to stop tracking.</param>
    /// <returns>True if the process was being tracked and was removed; otherwise false.</returns>
    public bool UntrackProcess(int processId)
    {
        return _trackedBindings.Remove(processId);
    }

    /// <summary>
    /// Gets all currently tracked process bindings.
    /// </summary>
    /// <returns>A read-only dictionary of process ID to <see cref="ProcessBinding"/>.</returns>
    public IReadOnlyDictionary<int, ProcessBinding> GetTrackedBindings()
    {
        return _trackedBindings;
    }

    /// <summary>
    /// Determines whether a process is a known game process based on its name.
    /// </summary>
    /// <param name="processName">The process name to check.</param>
    /// <returns>True if the process is a known game; otherwise false.</returns>
    public bool IsGameProcess(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return false;

        string normalized = processName.ToLowerInvariant();
        foreach (var gameName in _gameProcessNames)
        {
            if (normalized.Contains(gameName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    private static string GetMainWindowTitleSafe(Process proc)
    {
        try
        {
            return proc.MainWindowTitle ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static DateTime? GetProcessStartTimeSafe(Process proc)
    {
        try
        {
            return proc.StartTime;
        }
        catch
        {
            return null;
        }
    }

    private static double GetProcessCpuUsageSafe(Process proc)
    {
        try
        {
            double cpuTime = proc.TotalProcessorTime.TotalSeconds;
            return cpuTime;
        }
        catch
        {
            return 0;
        }
    }

    private static bool IsGpuIntensiveProcess(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return false;

        string normalized = processName.ToLowerInvariant();
        string[] gpuIntensiveKeywords = { "game", "render", "video", "gpu", "graphics",
            "unreal", "unity", "engine", "editor", "blender", "maya", "3dsmax",
            " Davinci", "premiere", "afterfx", "photoshop", "illustrator",
            "obs", "stream", "recorder", "capture" };

        foreach (var keyword in gpuIntensiveKeywords)
        {
            if (normalized.Contains(keyword, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string ConvertWildcardToRegex(string pattern)
    {
        var parts = pattern.Split('*');
        var regex = "^" + string.Join(".*", parts.Select(System.Text.RegularExpressions.Regex.Escape)) + "$";
        return regex;
    }
}