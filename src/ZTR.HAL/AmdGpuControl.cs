using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// AMD GPU control using ADL2 (AMD Display Library) with WMI-based fallback.
/// Provides comprehensive GPU hardware control including clock management, power limits,
/// FPS limiting, and iGPU-specific features.
/// </summary>
public class AmdGpuControl : IGpuControl
{
    private readonly int _gpuIndex;
    private readonly IAdl2Gpu _adl2;
    private bool _disposed;

    /// <inheritdoc />
    public bool IsNvidia => false;

    /// <inheritdoc />
    public bool IsAmd => true;

    /// <inheritdoc />
    public bool IsValid { get; private set; }

    /// <inheritdoc />
    public string FullName { get; private set; } = "AMD GPU";

    /// <inheritdoc />
    public int GpuIndex => _gpuIndex;

    /// <summary>
    /// Creates a new instance of the <see cref="AmdGpuControl"/> class.
    /// </summary>
    /// <param name="gpuIndex">The zero-based index of the GPU to control.</param>
    /// <param name="adl2">
    /// The ADL2 hardware abstraction. If null, the default <see cref="Adl2Gpu"/> implementation is used.
    /// </param>
    public AmdGpuControl(int gpuIndex = 0, IAdl2Gpu? adl2 = null)
    {
        _gpuIndex = gpuIndex;
        _adl2 = adl2 ?? new Adl2Gpu();
        Initialize();
    }

    private void Initialize()
    {
        IsValid = _adl2.IsAvailable && _gpuIndex < _adl2.GpuCount;
        FullName = _adl2.GetGpuName(_gpuIndex) ?? "AMD GPU";
    }

    /// <inheritdoc />
    public int? GetCurrentTemperature()
    {
        try
        {
            return _adl2.GetTemperature(_gpuIndex);
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
            return _adl2.GetHotspotTemperature(_gpuIndex);
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
            return _adl2.GetUsage(_gpuIndex);
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
            return _adl2.GetVramInfo(_gpuIndex);
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
            return _adl2.GetPower(_gpuIndex);
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
            return _adl2.SetClocks(_gpuIndex, coreOffset, memoryOffset);
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
            return _adl2.ResetClocks(_gpuIndex);
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
            return _adl2.SetPowerLimit(_gpuIndex, powerLimit);
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
            return _adl2.GetClockInfo(_gpuIndex);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the FPS limit for the GPU.
    /// </summary>
    /// <param name="fps">The FPS limit value, or 0 to disable.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetFPSLimit(int fps)
    {
        try
        {
            return _adl2.SetFPSLimit(_gpuIndex, fps);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current FPS limit.
    /// </summary>
    /// <returns>The FPS limit value, or null if not set or unavailable.</returns>
    public int? GetFPSLimit()
    {
        try
        {
            return _adl2.GetFPSLimit(_gpuIndex);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the iGPU power level.
    /// </summary>
    /// <param name="power">The power level in milliwatts.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetiGpuPower(int power)
    {
        try
        {
            return _adl2.SetiGpuPower(_gpuIndex, power);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets iGPU-specific sensor readings.
    /// </summary>
    /// <returns>A dictionary of sensor name to value, or null if unavailable.</returns>
    public IReadOnlyDictionary<string, double>? GetiGpuSensors()
    {
        try
        {
            return _adl2.GetiGpuSensors(_gpuIndex);
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
            return _adl2.GetPowerLimitRange(_gpuIndex);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the GPU fan speed manually.
    /// </summary>
    /// <param name="speed">The fan speed percentage (0-100).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetFanSpeed(int speed)
    {
        try
        {
            return _adl2.SetFanSpeed(_gpuIndex, Math.Clamp(speed, 0, 100));
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
            return _adl2.GetFanSpeed(_gpuIndex);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public void KillGpuApps()
    {
        try
        {
            _adl2.KillGpuApps(_gpuIndex);
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
            _adl2.Dispose();
        }
    }
}