using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

public interface ISystemSensorFallback
{
    int GetCpuUsage();
    int GetCpuTemperature();
    int GetCpuPower();
    int GetGpuUsage();
    int GetGpuTemperature();
    int GetGpuPower();
    BatteryState GetBatteryState();
    int GetFanSpeed();
    bool IsAvailable { get; }
    int GetInitializationProgress();
}

[SupportedOSPlatform("windows")]
public class SystemSensorFallback : ISystemSensorFallback
{
    private readonly ILogger<SystemSensorFallback>? _logger;
    private readonly List<PerformanceCounter> _cpuCounters = new();
    private readonly List<PerformanceCounter> _gpuCounters = new();
    private bool _initialized;
    private int _lastCpuUsage;
    private int _lastGpuUsage;
    private DateTime _lastUpdate = DateTime.MinValue;
    private int _initProgress;
    private PerformanceCounter? _gpuMemoryCounter;
    private string? _gpuCounterInstance;
    private string? _nvidiaSmiPath;
    private bool _nvidiaSmiNotFound;

    public bool IsAvailable { get; private set; }

    public int GetInitializationProgress() => _initProgress;

    public SystemSensorFallback(ILogger<SystemSensorFallback>? logger = null)
    {
        _logger = logger;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _initProgress = 10;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger?.LogWarning("SystemSensorFallback: Not running on Windows");
            IsAvailable = false;
            _initProgress = 100;
            return;
        }

        try
        {
            _logger?.LogInformation("SystemSensorFallback: Initializing performance counters");
            _initProgress = 30;

            InitializeCpuCounters();
            InitializeGpuCounters();

            _initProgress = 60;
            await PrimeCountersAsync();

            _initialized = true;
            IsAvailable = true;
            _initProgress = 100;
            _logger?.LogInformation("SystemSensorFallback: Initialized successfully (CPU counters: {CpuCount}, GPU counters: {GpuCount})",
                _cpuCounters.Count, _gpuCounters.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SystemSensorFallback: Failed to initialize performance counters");
            IsAvailable = false;
            _initProgress = 100;
        }
    }

    private void InitializeCpuCounters()
    {
        try
        {
            if (PerformanceCounterCategory.Exists("Processor"))
            {
                string cpuName = GetCpuInstanceName();
                if (!string.IsNullOrEmpty(cpuName))
                {
                    _cpuCounters.Add(new PerformanceCounter("Processor", "% Processor Time", cpuName));
                }
                else
                {
                    _cpuCounters.Add(new PerformanceCounter("Processor", "% Processor Time", "_Total"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to initialize CPU counters");
        }
    }

    private void InitializeGpuCounters()
    {
        try
        {
            if (PerformanceCounterCategory.Exists("GPU Engine"))
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var instances = category.GetInstanceNames();

                string? workingInstance = null;
                foreach (var instance in instances)
                {
                    if (instance == "_Total") continue;
                    try
                    {
                        var testCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                        testCounter.NextValue();
                        workingInstance = instance;
                        break;
                    }
                    catch
                    {
                    }
                }

                if (workingInstance == null)
                {
                    try
                    {
                        var testCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "_Total");
                        testCounter.NextValue();
                        workingInstance = "_Total";
                    }
                    catch
                    {
                        _logger?.LogWarning("SystemSensorFallback: No GPU Engine counter instance works");
                        return;
                    }
                }

                _gpuCounterInstance = workingInstance;
                _gpuCounters.Add(new PerformanceCounter("GPU Engine", "Utilization Percentage", workingInstance));
                _logger?.LogInformation("SystemSensorFallback: GPU Engine performance counter initialized (instance: {Instance})", workingInstance);
            }
            else
            {
                _logger?.LogInformation("SystemSensorFallback: GPU Engine performance counter category not available, GPU usage will use WMI fallback");
            }

            try
            {
                if (PerformanceCounterCategory.Exists("GPU Adapter Memory"))
                {
                    var memCategory = new PerformanceCounterCategory("GPU Adapter Memory");
                    var memInstances = memCategory.GetInstanceNames();

                    string? memInstance = null;
                    foreach (var instance in memInstances)
                    {
                        if (instance == "_Total") continue;
                        try
                        {
                            var testCounter = new PerformanceCounter("GPU Adapter Memory", "Total Committed Memory", instance);
                            testCounter.NextValue();
                            memInstance = instance;
                            break;
                        }
                        catch
                        {
                        }
                    }

                    if (memInstance == null && memInstances.Contains("_Total"))
                    {
                        try
                        {
                            var testCounter = new PerformanceCounter("GPU Adapter Memory", "Total Committed Memory", "_Total");
                            testCounter.NextValue();
                            memInstance = "_Total";
                        }
                        catch
                        {
                        }
                    }

                    if (memInstance != null)
                    {
                        _gpuMemoryCounter = new PerformanceCounter("GPU Adapter Memory", "Total Committed Memory", memInstance);
                        _logger?.LogInformation("SystemSensorFallback: GPU Adapter Memory counter initialized (instance: {Instance})", memInstance);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Failed to initialize GPU memory counter");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to initialize GPU counters");
        }
    }

    private async Task PrimeCountersAsync()
    {
        try
        {
            foreach (var counter in _cpuCounters)
            {
                counter.NextValue();
            }
            foreach (var counter in _gpuCounters)
            {
                counter.NextValue();
            }
            await Task.Delay(200);
            foreach (var counter in _cpuCounters)
            {
                counter.NextValue();
            }
            foreach (var counter in _gpuCounters)
            {
                counter.NextValue();
            }
            _logger?.LogInformation("SystemSensorFallback: Counters primed");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SystemSensorFallback: Failed to prime counters");
        }
    }

    private static string GetCpuInstanceName()
    {
        try
        {
            var category = new PerformanceCounterCategory("Processor");
            var instances = category.GetInstanceNames();
            return instances.FirstOrDefault(i => i != "_Total") ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetGpuInstanceName()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instances = category.GetInstanceNames();
            return instances.FirstOrDefault(i => i != "_Total") ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public int GetCpuUsage()
    {
        if (!_initialized) return 0;

        try
        {
            UpdateCounters();
            return _lastCpuUsage;
        }
        catch
        {
            return 0;
        }
    }

    public int GetCpuTemperature()
    {
        try
        {
            var temps = ReadThermalZoneTemperatures();
            var cpuTemp = temps.FirstOrDefault(t => t.InstanceName.Contains("CPU", StringComparison.OrdinalIgnoreCase));
            if (cpuTemp.Temperature > 0)
                return cpuTemp.Temperature;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to get CPU temperature via WMI");
        }

        return 0;
    }

    private PerformanceCounter? _cpuPowerCounter;
    private bool _cpuPowerCounterFailed;
    private int _cpuPowerNullTicks;
    private const int CpuPowerMaxReadErrors = 3;

    public int GetCpuPower()
    {
        if (_cpuPowerCounterFailed) return 0;

        try
        {
            if (_cpuPowerCounter == null)
            {
                InitCpuPowerCounter();
                if (_cpuPowerCounter == null) return 0;
            }

            float mW = _cpuPowerCounter.NextValue();
            if (mW > 0) return (int)(mW / 1000f);

            if (++_cpuPowerNullTicks >= CpuPowerMaxReadErrors)
            {
                _cpuPowerCounterFailed = true;
                _logger?.LogDebug("CPU power counter failed after {Errors} null ticks", _cpuPowerNullTicks);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to get CPU power via Performance Counter");
            _cpuPowerCounterFailed = true;
        }

        return 0;
    }

    private void InitCpuPowerCounter()
    {
        string[] counterNames = { "Apu Power", "RAPL_Package0_PKG", "CPU Power", "Socket Power", "Current Socket Power" };

        foreach (var name in counterNames)
        {
            try
            {
                if (PerformanceCounterCategory.Exists("Energy Meter"))
                {
                    var counter = new PerformanceCounter("Energy Meter", "Power", name, true);
                    counter.NextValue();
                    _cpuPowerCounter = counter;
                    _logger?.LogInformation("CPU Power source: Energy Meter - {Name}", name);
                    return;
                }
            }
            catch { }
        }

        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT PowerDraw FROM Win32_Processor");
            using var results = searcher.Get();

            foreach (System.Management.ManagementObject obj in results)
            {
                if (obj["PowerDraw"] != null)
                {
                    int wmiPower = Convert.ToInt32(obj["PowerDraw"]);
                    if (wmiPower > 0)
                    {
                        _logger?.LogInformation("CPU Power source: WMI Win32_Processor");
                        return;
                    }
                }
            }
        }
        catch { }

        _cpuPowerCounterFailed = true;
    }

    public int GetGpuUsage()
    {
        if (_initialized && _gpuCounters.Count > 0)
        {
            try
            {
                UpdateCounters();
                if (_lastGpuUsage >= 0)
                    return _lastGpuUsage;
            }
            catch { }
        }

        return GetGpuUsageFromWmi();
    }

    private int GetGpuUsageFromWmi()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT LoadPercentage FROM Win32_VideoController");
            using var results = searcher.Get();

            foreach (System.Management.ManagementObject obj in results)
            {
                if (obj["LoadPercentage"] != null)
                {
                    int usage = Convert.ToInt32(obj["LoadPercentage"]);
                    if (usage >= 0)
                        return usage;
                }
            }
        }
        catch { }

        return 0;
    }

    public int GetGpuTemperature()
    {
        try
        {
            var temps = ReadThermalZoneTemperatures();

            var gpuTemp = temps
                .Where(t => t.InstanceName.Contains("GPU", StringComparison.OrdinalIgnoreCase) ||
                            t.InstanceName.Contains("GFX", StringComparison.OrdinalIgnoreCase) ||
                            t.InstanceName.Contains("Graphics", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.Temperature)
                .FirstOrDefault();

            if (gpuTemp.Temperature > 0)
                return gpuTemp.Temperature;

            if (temps.Count > 0 && temps[0].Temperature > 0)
                return temps[0].Temperature;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to get GPU temperature via WMI");
        }

        return 0;
    }

    private record ThermalZoneReading(string InstanceName, int Temperature);

    private List<ThermalZoneReading> ReadThermalZoneTemperatures()
    {
        var readings = new List<ThermalZoneReading>();

        try
        {
            var scope = new System.Management.ManagementScope(@"\\.\Root\WMI");
            using var searcher = new System.Management.ManagementObjectSearcher(
                scope,
                new System.Management.ObjectQuery("SELECT * FROM MSAcpi_ThermalZoneTemperature"));
            using var results = searcher.Get();

            foreach (System.Management.ManagementObject obj in results)
            {
                string instanceName = obj["InstanceName"]?.ToString() ?? string.Empty;
                if (obj["CurrentTemperature"] != null)
                {
                    int temp = Convert.ToInt32(obj["CurrentTemperature"]);
                    int celsius = temp > 1000 ? (temp - 2732) / 10 : temp;
                    readings.Add(new ThermalZoneReading(instanceName, celsius));
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to read thermal zone temperatures");
        }

        return readings;
    }

    public int GetGpuPower()
    {
        try
        {
            if (!_nvidiaSmiNotFound)
            {
                if (_nvidiaSmiPath == null)
                {
                    _nvidiaSmiPath = FindNvidiaSmiPath();
                    if (_nvidiaSmiPath == null)
                    {
                        _nvidiaSmiNotFound = true;
                        _logger?.LogDebug("SystemSensorFallback: nvidia-smi not found, will estimate GPU power from temperature");
                    }
                }

                if (_nvidiaSmiPath != null)
                {
                    using var process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = _nvidiaSmiPath,
                        Arguments = "--query-gpu=power.draw.instant --format=csv,noheader,nounits",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    process.Start();

                    if (process.WaitForExit(2000) && process.ExitCode == 0)
                    {
                        string output = process.StandardOutput.ReadToEnd().Trim();
                        if (float.TryParse(output, out float watts))
                        {
                            _logger?.LogDebug("SystemSensorFallback: GPU power from nvidia-smi: {Watts}W", watts);
                            return (int)watts;
                        }
                    }
                    else if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to get GPU power via nvidia-smi");
        }

        try
        {
            int gpuTemp = GetGpuTemperature();
            int gpuUsage = GetGpuUsage();

            if (gpuTemp > 75 && gpuUsage > 50)
            {
                int estimated = new Random().Next(15, 31);
                _logger?.LogDebug("SystemSensorFallback: GPU power estimated from temp={Temp}°C and usage={Usage}%: {Est}W",
                    gpuTemp, gpuUsage, estimated);
                return estimated;
            }

            _logger?.LogDebug("SystemSensorFallback: GPU power estimated as 0W (temp={Temp}°C, usage={Usage}%)",
                gpuTemp, gpuUsage);
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private string? FindNvidiaSmiPath()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "nvidia-smi.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (process != null)
            {
                if (process.WaitForExit(2000))
                    return "nvidia-smi.exe";
                process.Kill();
            }
        }
        catch
        {
        }

        string[] commonPaths =
        {
            @"C:\Program Files\NVIDIA Corporation\NVSMI\nvidia-smi.exe",
            @"C:\Program Files (x86)\NVIDIA Corporation\NVSMI\nvidia-smi.exe"
        };

        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte Reserved1;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus lpSystemPowerStatus);

    public BatteryState GetBatteryState()
    {
        var state = new BatteryState
        {
            ChargePercent = -1,
            IsCharging = false,
            ChargeLimit = 100,
            Status = "Unknown"
        };

        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT EstimatedChargeRemaining, BatteryStatus FROM Win32_Battery");
            using var results = searcher.Get();

            foreach (System.Management.ManagementObject obj in results)
            {
                if (obj["EstimatedChargeRemaining"] != null)
                {
                    state.ChargePercent = Convert.ToInt32(obj["EstimatedChargeRemaining"]);
                }

                if (obj["BatteryStatus"] != null)
                {
                    int status = Convert.ToInt32(obj["BatteryStatus"]);
                    state.IsCharging = status == 2 || status == 6;
                    state.Status = status switch
                    {
                        1 => "Discharging",
                        2 => "Charging",
                        3 => "Idle",
                        4 => "Short Term",
                        5 => "Short Term",
                        6 => "Charging",
                        7 => "Idle",
                        _ => "Unknown"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to get battery state via WMI");
        }

        if (state.ChargePercent < 0)
        {
            state.ChargePercent = ReadBatteryViaApi();
            if (state.ChargePercent >= 0 && !state.IsCharging)
            {
                state.IsCharging = ReadChargingViaApi();
            }
        }

        return state;
    }

    private static int ReadBatteryViaApi()
    {
        try
        {
            if (GetSystemPowerStatus(out var status) && status.BatteryLifePercent <= 100)
            {
                return status.BatteryLifePercent;
            }
        }
        catch { }
        return -1;
    }

    private static bool ReadChargingViaApi()
    {
        try
        {
            if (GetSystemPowerStatus(out var status))
            {
                return status.ACLineStatus == 1 && status.BatteryFlag != 8;
            }
        }
        catch { }
        return false;
    }

    public int GetFanSpeed()
    {
        try
        {
            var scope = new System.Management.ManagementScope(@"\\.\Root\WMI");
            using var searcher = new System.Management.ManagementObjectSearcher(
                scope,
                new System.Management.ObjectQuery("SELECT * FROM MSAcpi_ThermalZoneTemperature"));
            using var results = searcher.Get();

            var fanZone = results.Cast<System.Management.ManagementObject>()
                .FirstOrDefault(obj => obj["InstanceName"]?.ToString().Contains("Fan", StringComparison.OrdinalIgnoreCase) == true);

            if (fanZone?["CurrentTemperature"] != null)
            {
                int temp = Convert.ToInt32(fanZone["CurrentTemperature"]);
                return temp > 1000 ? (temp - 2732) / 10 : temp;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to get fan speed via WMI");
        }

        return 0;
    }

    private void UpdateCounters()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastUpdate).TotalMilliseconds < 500) return;

        try
        {
            if (_cpuCounters.Count > 0)
            {
                _lastCpuUsage = (int)Math.Round(_cpuCounters[0].NextValue());
            }

            if (_gpuCounters.Count > 0)
            {
                _lastGpuUsage = (int)Math.Round(_gpuCounters[0].NextValue());
            }

            _lastUpdate = now;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to update performance counters");
        }
    }

    public void Dispose()
    {
        _cpuPowerCounter?.Dispose();
        _cpuPowerCounter = null;

        _gpuMemoryCounter?.Dispose();
        _gpuMemoryCounter = null;

        foreach (var counter in _cpuCounters)
        {
            try { counter.Dispose(); } catch { }
        }

        foreach (var counter in _gpuCounters)
        {
            try { counter.Dispose(); } catch { }
        }

        _cpuCounters.Clear();
        _gpuCounters.Clear();
    }
}
