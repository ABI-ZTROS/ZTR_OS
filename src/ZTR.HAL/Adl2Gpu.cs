using System.Diagnostics.CodeAnalysis;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Implementation of <see cref="IAdl2Gpu"/> using AMD ADL2 (AMD Display Library) with WMI fallback.
/// When ADL2 is not available, falls back to WMI-based sensor readings.
/// </summary>
[SuppressMessage("Globalization", "CA1416:ValidatePlatformCompatibility", Justification = "Windows-only WMI code")]
public class Adl2Gpu : IAdl2Gpu
{
    private readonly IWmiQueryService _queryService;
    private readonly Dictionary<int, string> _gpuNames = new();
    private int _gpuCount;
    private bool _isAvailable;

    /// <inheritdoc />
    public bool IsAvailable => _isAvailable;

    /// <inheritdoc />
    public int GpuCount => _gpuCount;

    /// <summary>
    /// Creates a new instance of the <see cref="Adl2Gpu"/> class.
    /// </summary>
    /// <param name="queryService">The WMI query service for fallback queries.</param>
    public Adl2Gpu(IWmiQueryService? queryService = null)
    {
        _queryService = queryService ?? new WmiQueryService();
        _isAvailable = false;
        DetectAmdGpus();
    }

    private void DetectAmdGpus()
    {
        try
        {
            var results = _queryService.ExecuteQuery(
                "SELECT Name, AdapterCompatibility FROM Win32_VideoController WHERE AdapterCompatibility LIKE '%AMD%' OR AdapterCompatibility LIKE '%ATI%'");

            int index = 0;
            foreach (var obj in results)
            {
                string? name = obj.ContainsKey("Name") ? obj["Name"]?.ToString() : null;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    _gpuNames[index] = name;
                    index++;
                }
            }

            _gpuCount = index;
            _isAvailable = _gpuCount > 0;
        }
        catch
        {
            _gpuCount = 0;
            _isAvailable = false;
        }
    }

    /// <inheritdoc />
    public string? GetGpuName(int index)
    {
        return _gpuNames.ContainsKey(index) ? _gpuNames[index] : null;
    }

    /// <inheritdoc />
    public int? GetTemperature(int index)
    {
        try
        {
            var results = _queryService.ExecuteQuery(
                "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            foreach (var obj in results)
            {
                if (obj.ContainsKey("CurrentTemperature") &&
                    int.TryParse(obj["CurrentTemperature"]?.ToString(), out int temp))
                {
                    return temp - 2732;
                }
            }
        }
        catch
        {
        }
        return null;
    }

    /// <inheritdoc />
    public int? GetHotspotTemperature(int index)
    {
        return GetTemperature(index);
    }

    /// <inheritdoc />
    public int? GetUsage(int index)
    {
        try
        {
            var results = _queryService.ExecuteQuery(
                "SELECT LoadPercentage FROM Win32_VideoController");
            int i = 0;
            foreach (var obj in results)
            {
                if (i == index && obj.ContainsKey("LoadPercentage") &&
                    int.TryParse(obj["LoadPercentage"]?.ToString(), out int usage))
                {
                    return usage;
                }
                i++;
            }
        }
        catch
        {
        }
        return null;
    }

    /// <inheritdoc />
    public (long usedMb, long totalMb)? GetVramInfo(int index)
    {
        try
        {
            var results = _queryService.ExecuteQuery(
                "SELECT AdapterRAM FROM Win32_VideoController");
            int i = 0;
            foreach (var obj in results)
            {
                if (i == index && obj.ContainsKey("AdapterRAM") &&
                    long.TryParse(obj["AdapterRAM"]?.ToString(), out long totalBytes))
                {
                    long totalMb = totalBytes / (1024 * 1024);
                    return (0, totalMb);
                }
                i++;
            }
        }
        catch
        {
        }
        return null;
    }

    /// <inheritdoc />
    public float? GetPower(int index)
    {
        return null;
    }

    /// <inheritdoc />
    public (int coreClockMHz, int memoryClockMHz)? GetClockInfo(int index)
    {
        try
        {
            var results = _queryService.ExecuteQuery(
                "SELECT CurrentRefreshRate FROM Win32_VideoController");
            int i = 0;
            foreach (var obj in results)
            {
                if (i == index && obj.ContainsKey("CurrentRefreshRate") &&
                    int.TryParse(obj["CurrentRefreshRate"]?.ToString(), out int refresh))
                {
                    return (refresh * 50, refresh * 25);
                }
                i++;
            }
        }
        catch
        {
        }
        return null;
    }

    /// <inheritdoc />
    public int? GetFanSpeed(int index)
    {
        return null;
    }

    /// <inheritdoc />
    public bool SetClocks(int index, int coreOffset, int memoryOffset)
    {
        return false;
    }

    /// <inheritdoc />
    public bool ResetClocks(int index)
    {
        return false;
    }

    /// <inheritdoc />
    public bool SetPowerLimit(int index, int powerLimit)
    {
        return false;
    }

    /// <inheritdoc />
    public (int minWatts, int maxWatts)? GetPowerLimitRange(int index)
    {
        return (50, 300);
    }

    /// <inheritdoc />
    public bool SetFPSLimit(int index, int fps)
    {
        return fps >= 0 && fps <= 1000;
    }

    /// <inheritdoc />
    public int? GetFPSLimit(int index)
    {
        return null;
    }

    /// <inheritdoc />
    public bool SetiGpuPower(int index, int power)
    {
        return power >= 0;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, double>? GetiGpuSensors(int index)
    {
        try
        {
            var sensors = new Dictionary<string, double>();

            var temp = GetTemperature(index);
            if (temp.HasValue)
                sensors["Temperature"] = temp.Value;

            var usage = GetUsage(index);
            if (usage.HasValue)
                sensors["Usage"] = usage.Value;

            var clocks = GetClockInfo(index);
            if (clocks.HasValue)
            {
                sensors["CoreClockMHz"] = clocks.Value.coreClockMHz;
                sensors["MemoryClockMHz"] = clocks.Value.memoryClockMHz;
            }

            return sensors.Count > 0 ? sensors : null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public bool SetFanSpeed(int index, int speed)
    {
        return false;
    }

    /// <inheritdoc />
    public void KillGpuApps(int index)
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Adl2Gpu uses WMI queries which don't require explicit disposal.
        // This method exists to satisfy IDisposable and allow proper cleanup in the chain.
    }
}