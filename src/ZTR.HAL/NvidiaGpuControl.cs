using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// NVIDIA GPU control using NVAPI and NVML. Provides comprehensive GPU hardware control
/// including clock management, power limits, fan control, and real-time sensor monitoring.
/// </summary>
public class NvidiaGpuControl : IGpuControl
{
    private readonly int _gpuIndex;
    private readonly INvApiGpu _nvApi;
    private bool _disposed;

    /// <inheritdoc />
    public bool IsNvidia => true;

    /// <inheritdoc />
    public bool IsAmd => false;

    /// <inheritdoc />
    public bool IsValid { get; private set; }

    /// <inheritdoc />
    public string FullName { get; private set; } = "NVIDIA GPU";

    /// <inheritdoc />
    public int GpuIndex => _gpuIndex;

    /// <summary>
    /// Creates a new instance of the <see cref="NvidiaGpuControl"/> class.
    /// </summary>
    /// <param name="gpuIndex">The zero-based index of the GPU to control.</param>
    /// <param name="nvApi">
    /// The NVAPI hardware abstraction. If null, the default <see cref="NvApiGpu"/> implementation is used.
    /// </param>
    public NvidiaGpuControl(int gpuIndex = 0, INvApiGpu? nvApi = null)
    {
        _gpuIndex = gpuIndex;
        _nvApi = nvApi ?? new NvApiGpu();
        Initialize();
    }

    private void Initialize()
    {
        IsValid = _nvApi.IsAvailable && _gpuIndex < _nvApi.GpuCount;
        FullName = _nvApi.GetGpuName(_gpuIndex) ?? "NVIDIA GPU";
    }

    /// <inheritdoc />
    public int? GetCurrentTemperature()
    {
        try
        {
            return _nvApi.GetTemperature(_gpuIndex);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public int? GetHotspotTemperature()
    {
        try
        {
            return _nvApi.GetHotspotTemperature(_gpuIndex);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public int? GetGpuUse()
    {
        try
        {
            return _nvApi.GetUsage(_gpuIndex);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public (long usedMb, long totalMb)? GetVramInfo()
    {
        try
        {
            return _nvApi.GetVramInfo(_gpuIndex);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public float? GetGpuPower()
    {
        try
        {
            return _nvApi.GetPower(_gpuIndex);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public bool SetClocks(int coreOffset, int memoryOffset)
    {
        try
        {
            return _nvApi.SetClocks(_gpuIndex, coreOffset, memoryOffset);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool ResetClocks()
    {
        try
        {
            return _nvApi.ResetClocks(_gpuIndex);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool SetPowerLimit(int powerLimit)
    {
        try
        {
            return _nvApi.SetPowerLimit(_gpuIndex, powerLimit);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Limits the maximum GPU clock speed.
    /// </summary>
    /// <param name="clock">The maximum clock speed in MHz.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetMaxGpuClock(int clock)
    {
        try
        {
            return _nvApi.SetMaxGpuClock(_gpuIndex, clock);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current clock speeds of the GPU.
    /// </summary>
    /// <returns>
    /// A tuple of (coreClockMHz, memoryClockMHz), or null if the information is unavailable.
    /// </returns>
    public (int coreClockMHz, int memoryClockMHz)? GetClockInfo()
    {
        try
        {
            return _nvApi.GetClockInfo(_gpuIndex);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the GPU fan speed manually via NVAPI.
    /// </summary>
    /// <param name="speed">The fan speed percentage (0-100).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetFanSpeed(int speed)
    {
        try
        {
            return _nvApi.SetFanSpeed(_gpuIndex, Math.Clamp(speed, 0, 100));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current GPU fan speed.
    /// </summary>
    /// <returns>The fan speed percentage (0-100), or null if unavailable.</returns>
    public int? GetFanSpeed()
    {
        try
        {
            return _nvApi.GetFanSpeed(_gpuIndex);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the power limit range for the GPU.
    /// </summary>
    /// <returns>
    /// A tuple of (minWatts, maxWatts), or null if the information is unavailable.
    /// </returns>
    public (int minWatts, int maxWatts)? GetPowerLimitRange()
    {
        try
        {
            return _nvApi.GetPowerLimitRange(_gpuIndex);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the list of supported power modes for this GPU.
    /// </summary>
    /// <returns>A read-only list of power mode names.</returns>
    public IReadOnlyList<string> GetSupportedPowerModes()
    {
        try
        {
            return _nvApi.GetSupportedPowerModes(_gpuIndex);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public void KillGpuApps()
    {
        try
        {
            _nvApi.KillGpuApps(_gpuIndex);
        }
        catch
        {
        }
    }

    /// <inheritdoc />
    public GpuState GetState()
    {
        var clocks = GetClockInfo();
        var vram = GetVramInfo();

        return new GpuState
        {
            Temperature = GetCurrentTemperature() ?? 0,
            HotspotTemperature = GetHotspotTemperature() ?? 0,
            Usage = GetGpuUse() ?? 0,
            Power = (int?)GetGpuPower() ?? 0,
            UsedVramMB = vram?.usedMb ?? 0,
            TotalVramMB = vram?.totalMb ?? 0,
            CoreClockMHz = clocks?.coreClockMHz ?? 0,
            MemoryClockMHz = clocks?.memoryClockMHz ?? 0
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _nvApi.Dispose();
        }
    }
}