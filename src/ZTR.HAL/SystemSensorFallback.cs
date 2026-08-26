using System.Diagnostics;
using System.Runtime.InteropServices;
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
}

public class SystemSensorFallback : ISystemSensorFallback
{
    private readonly ILogger<SystemSensorFallback>? _logger;
    private readonly List<PerformanceCounter> _cpuCounters = new();
    private readonly List<PerformanceCounter> _gpuCounters = new();
    private bool _initialized;
    private int _lastCpuUsage;
    private int _lastGpuUsage;
    private DateTime _lastUpdate = DateTime.MinValue;

    public bool IsAvailable { get; private set; }

    public SystemSensorFallback(ILogger<SystemSensorFallback>? logger = null)
    {
        _logger = logger;
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger?.LogWarning("SystemSensorFallback: Not running on Windows");
                IsAvailable = false;
                return;
            }

            _logger?.LogInformation("SystemSensorFallback: Initializing performance counters");

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

            if (PerformanceCounterCategory.Exists("GPU Engine"))
            {
                string gpuName = GetGpuInstanceName();
                if (!string.IsNullOrEmpty(gpuName))
                {
                    _gpuCounters.Add(new PerformanceCounter("GPU Engine", "Utilization Percentage", gpuName));
                }
                else
                {
                    _gpuCounters.Add(new PerformanceCounter("GPU Engine", "Utilization Percentage", "_Total"));
                }
            }

            PrimeCounters();

            _initialized = true;
            IsAvailable = _cpuCounters.Count > 0 || _gpuCounters.Count > 0;
            _logger?.LogInformation("SystemSensorFallback: Initialized successfully (CPU counters: {CpuCount}, GPU counters: {GpuCount})",
                _cpuCounters.Count, _gpuCounters.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SystemSensorFallback: Failed to initialize performance counters");
            IsAvailable = false;
        }
    }

    private void PrimeCounters()
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
            Task.Delay(100).Wait();
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
            using var scope = new System.Management.ManagementScope(@"\\.\WMI");
            using var searcher = new System.Management.ManagementObjectSearcher(
                scope,
                new System.Management.ObjectQuery("SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature"));
            using var results = searcher.Get();

            foreach (System.Management.ManagementObject obj in results)
            {
                if (obj["CurrentTemperature"] != null)
                {
                    int temp = Convert.ToInt32(obj["CurrentTemperature"]);
                    return temp > 1000 ? (temp - 2732) / 10 : temp;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to get CPU temperature via WMI Root\\WMI");
        }

        try
        {
            using var scope = new System.Management.ManagementScope(@"\\.\Root\WMI");
            using var searcher = new System.Management.ManagementObjectSearcher(
                scope,
                new System.Management.ObjectQuery("SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature"));
            using var results = searcher.Get();

            foreach (System.Management.ManagementObject obj in results)
            {
                if (obj["CurrentTemperature"] != null)
                {
                    int temp = Convert.ToInt32(obj["CurrentTemperature"]);
                    return temp > 1000 ? (temp - 2732) / 10 : temp;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to get CPU temperature via WMI Root\\WMI");
        }

        return 0;
    }

    public int GetCpuPower()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT PowerDraw FROM Win32_Processor");
            using var results = searcher.Get();

            foreach (System.Management.ManagementObject obj in results)
            {
                if (obj["PowerDraw"] != null)
                {
                    return Convert.ToInt32(obj["PowerDraw"]);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to get CPU power via WMI");
        }

        return 0;
    }

    public int GetGpuUsage()
    {
        if (!_initialized || _gpuCounters.Count == 0) return 0;

        try
        {
            UpdateCounters();
            return _lastGpuUsage;
        }
        catch
        {
            return 0;
        }
    }

    public int GetGpuTemperature()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature WHERE InstanceName LIKE '%GPU%'");
            using var results = searcher.Get();

            foreach (System.Management.ManagementObject obj in results)
            {
                if (obj["CurrentTemperature"] != null)
                {
                    int temp = Convert.ToInt32(obj["CurrentTemperature"]);
                    return temp > 1000 ? (temp - 2732) / 10 : temp;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to get GPU temperature via WMI");
        }

        return 0;
    }

    public int GetGpuPower()
    {
        return 0;
    }

    public BatteryState GetBatteryState()
    {
        var state = new BatteryState
        {
            ChargePercent = 100,
            IsCharging = false,
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
                    state.IsCharging = status == 2;
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

        return state;
    }

    public int GetFanSpeed()
    {
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
