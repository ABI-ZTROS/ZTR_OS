using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Comprehensive GPU tuning service that provides clock, power, temperature, and voltage
/// control for ASUS ROG systems. Wraps <see cref="IGpuControl"/> for GPU-specific operations
/// and <see cref="AsusAcpi"/> for ACPI-level hardware communication via PPT_GPUC0 (GPU power)
/// and PPT_GPUC2 (GPU temperature target).
/// </summary>
public class GpuTuningService : IDisposable
{
    private readonly IGpuControl _gpuControl;
    private readonly AsusAcpi _acpi;
    private readonly ILogger<GpuTuningService>? _logger;
    private readonly object _lock = new();
    private bool _disposed;

    private GpuTuningState _state = new();

    /// <summary>
    /// Gets the current GPU tuning state.
    /// </summary>
    public GpuTuningState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether this service is controlling an NVIDIA GPU.
    /// </summary>
    public bool IsNvidia => _gpuControl.IsNvidia;

    /// <summary>
    /// Gets a value indicating whether this service is controlling an AMD GPU.
    /// </summary>
    public bool IsAmd => _gpuControl.IsAmd;

    /// <summary>
    /// Gets the underlying GPU control interface.
    /// </summary>
    public IGpuControl GpuControl => _gpuControl;

    /// <summary>
    /// Creates a new instance of the <see cref="GpuTuningService"/> class.
    /// </summary>
    /// <param name="gpuControl">The GPU control interface for clock and power operations.</param>
    /// <param name="acpi">The ASUS ACPI interface for hardware-level GPU power and temperature control.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="gpuControl"/> or <paramref name="acpi"/> is null.
    /// </exception>
    public GpuTuningService(IGpuControl gpuControl, AsusAcpi acpi, ILogger<GpuTuningService>? logger = null)
    {
        _gpuControl = gpuControl ?? throw new ArgumentNullException(nameof(gpuControl));
        _acpi = acpi ?? throw new ArgumentNullException(nameof(acpi));
        _logger = logger;
    }

    /// <summary>
    /// Sets the GPU core clock offset in MHz.
    /// Positive values increase the clock, negative values decrease it.
    /// </summary>
    /// <param name="offset">The core clock offset in MHz.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetCoreClockOffset(int offset)
    {
        lock (_lock)
        {
            try
            {
                _logger?.LogInformation("Setting GPU core clock offset to {Offset} MHz", offset);

                bool result = _gpuControl.SetClocks(offset, _state.MemoryClockOffset);

                if (result)
                {
                    _state.CoreClockOffset = offset;
                    _logger?.LogInformation("GPU core clock offset set to {Offset} MHz successfully", offset);
                }
                else
                {
                    _logger?.LogWarning("Failed to set GPU core clock offset to {Offset} MHz", offset);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting GPU core clock offset to {Offset} MHz", offset);
                return false;
            }
        }
    }

    /// <summary>
    /// Sets the GPU memory clock offset in MHz.
    /// Positive values increase the clock, negative values decrease it.
    /// </summary>
    /// <param name="offset">The memory clock offset in MHz.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetMemoryClockOffset(int offset)
    {
        lock (_lock)
        {
            try
            {
                _logger?.LogInformation("Setting GPU memory clock offset to {Offset} MHz", offset);

                bool result = _gpuControl.SetClocks(_state.CoreClockOffset, offset);

                if (result)
                {
                    _state.MemoryClockOffset = offset;
                    _logger?.LogInformation("GPU memory clock offset set to {Offset} MHz successfully", offset);
                }
                else
                {
                    _logger?.LogWarning("Failed to set GPU memory clock offset to {Offset} MHz", offset);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting GPU memory clock offset to {Offset} MHz", offset);
                return false;
            }
        }
    }

    /// <summary>
    /// Sets both core and memory clock offsets simultaneously.
    /// </summary>
    /// <param name="coreOffset">The core clock offset in MHz.</param>
    /// <param name="memoryOffset">The memory clock offset in MHz.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetBothClocks(int coreOffset, int memoryOffset)
    {
        lock (_lock)
        {
            try
            {
                _logger?.LogInformation("Setting GPU clocks — core: {Core} MHz, memory: {Mem} MHz",
                    coreOffset, memoryOffset);

                bool result = _gpuControl.SetClocks(coreOffset, memoryOffset);

                if (result)
                {
                    _state.CoreClockOffset = coreOffset;
                    _state.MemoryClockOffset = memoryOffset;
                    _logger?.LogInformation("GPU clocks set successfully — core: {Core} MHz, memory: {Mem} MHz",
                        coreOffset, memoryOffset);
                }
                else
                {
                    _logger?.LogWarning("Failed to set GPU clocks — core: {Core} MHz, memory: {Mem} MHz",
                        coreOffset, memoryOffset);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting GPU clocks — core: {Core} MHz, memory: {Mem} MHz",
                    coreOffset, memoryOffset);
                return false;
            }
        }
    }

    /// <summary>
    /// Sets the GPU power limit in watts. Uses both the GPU API for direct power limit
    /// control and the ACPI interface for PPT_GPUC0 (GPU Boost) hardware-level power adjustment.
    /// </summary>
    /// <param name="watts">The power limit in watts. Clamped to a valid range (0-500W).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetPowerLimit(int watts)
    {
        lock (_lock)
        {
            try
            {
                watts = Math.Clamp(watts, 0, 500);
                _logger?.LogInformation("Setting GPU power limit to {Watts}W", watts);

                bool gpuApiResult = _gpuControl.SetPowerLimit(watts);

                int acpiResult = _acpi.DeviceSet(AsusDevice.PPT_GPUC0, watts, $"SetGpuPower({watts}W)");

                bool result = gpuApiResult || acpiResult == 1;

                if (result)
                {
                    _state.PowerLimit = watts;
                    _logger?.LogInformation("GPU power limit set to {Watts}W successfully (GPU API: {GpuResult}, ACPI: {AcpiResult})",
                        watts, gpuApiResult, acpiResult == 1);
                }
                else
                {
                    _logger?.LogWarning("Failed to set GPU power limit to {Watts}W (GPU API: {GpuResult}, ACPI: {AcpiResult})",
                        watts, gpuApiResult, acpiResult == 1);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting GPU power limit to {Watts}W", watts);
                return false;
            }
        }
    }

    /// <summary>
    /// Sets the GPU temperature target (junction temperature limit) in degrees Celsius
    /// via the ACPI PPT_GPUC2 interface. This controls the hardware-level temperature
    /// threshold at which the GPU begins throttling.
    /// </summary>
    /// <param name="tempC">The temperature target in degrees Celsius. Clamped to a valid range (40-100°C).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetTemperatureLimit(int tempC)
    {
        lock (_lock)
        {
            try
            {
                tempC = Math.Clamp(tempC, 40, 100);
                _logger?.LogInformation("Setting GPU temperature target to {Temp}°C", tempC);

                int result = _acpi.DeviceSet(AsusDevice.PPT_GPUC2, tempC, $"SetGpuTempTarget({tempC}°C)");

                if (result == 1)
                {
                    _state.TemperatureLimit = tempC;
                    _logger?.LogInformation("GPU temperature target set to {Temp}°C successfully", tempC);
                }
                else
                {
                    _logger?.LogWarning("Failed to set GPU temperature target to {Temp}°C (ACPI returned: {Result})",
                        tempC, result);
                }

                return result == 1;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting GPU temperature target to {Temp}°C", tempC);
                return false;
            }
        }
    }

    /// <summary>
    /// Sets the Dynamic Boost level for GPU power allocation. Dynamic Boost allows
    /// the GPU to draw additional power from the CPU's power budget during heavy GPU loads.
    /// </summary>
    /// <param name="level">The Dynamic Boost level in watts. Valid values are 0 (off), 5, 15, or 20.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetDynamicBoost(int level)
    {
        lock (_lock)
        {
            try
            {
                int normalizedLevel = level switch
                {
                    0 => 0,
                    5 => 1,
                    15 => 2,
                    20 => 3,
                    _ => throw new ArgumentOutOfRangeException(nameof(level),
                        $"Invalid Dynamic Boost level: {level}. Valid values are 0, 5, 15, or 20.")
                };

                _logger?.LogInformation("Setting Dynamic Boost to {Level}W (normalized value: {Value})",
                    level, normalizedLevel);

                int result = _acpi.DeviceSet(AsusDevice.PPT_GPUC0, normalizedLevel,
                    $"SetDynamicBoost({level}W)");

                if (result == 1)
                {
                    _state.DynamicBoostLevel = level;
                    _logger?.LogInformation("Dynamic Boost set to {Level}W successfully", level);
                }
                else
                {
                    _logger?.LogWarning("Failed to set Dynamic Boost to {Level}W (ACPI returned: {Result})",
                        level, result);
                }

                return result == 1;
            }
            catch (ArgumentOutOfRangeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting Dynamic Boost to {Level}W", level);
                return false;
            }
        }
    }

    /// <summary>
    /// Sets the GPU voltage offset via clock-based undervolting.
    /// Direct voltage control via NvAPI is not available in this version;
    /// instead, voltage adjustments are approximated through clock offsets.
    /// Undervolting (negative values) reduces core clock proportionally.
    /// </summary>
    /// <param name="offset">The voltage offset as a percentage (e.g., -10 for -10%, 10 for +10%). Clamped to -50..+50.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetVoltageOffset(int offset)
    {
        lock (_lock)
        {
            try
            {
                offset = Math.Clamp(offset, -50, 50);
                _logger?.LogInformation("Setting GPU voltage offset to {Offset}% via clock-based adjustment", offset);

                int clockAdjustment = (int)(offset * 2.5);

                bool result = _gpuControl.SetClocks(
                    _state.CoreClockOffset + clockAdjustment,
                    _state.MemoryClockOffset);

                if (result)
                {
                    _state.VoltageOffset = offset;
                    _state.CoreClockOffset += clockAdjustment;
                    _logger?.LogInformation("GPU voltage offset set to {Offset}% (core clock adjusted by {Adjust}MHz)", offset, clockAdjustment);
                }
                else
                {
                    _logger?.LogWarning("Failed to set GPU voltage offset to {Offset}%", offset);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting GPU voltage offset to {Offset}%", offset);
                return false;
            }
        }
    }

    /// <summary>
    /// Resets all GPU tuning settings to their default values. This includes
    /// clock offsets, power limit, temperature target, dynamic boost, and voltage offset.
    /// </summary>
    /// <returns>True if all reset operations succeeded; otherwise false.</returns>
    public bool ResetAll()
    {
        lock (_lock)
        {
            bool allSucceeded = true;

            _logger?.LogInformation("Resetting all GPU tuning settings to defaults...");

            try
            {
                bool clockReset = _gpuControl.ResetClocks();
                if (!clockReset)
                {
                    _logger?.LogWarning("Failed to reset GPU clocks to defaults");
                    allSucceeded = false;
                }
                else
                {
                    _logger?.LogInformation("GPU clocks reset to defaults");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error resetting GPU clocks");
                allSucceeded = false;
            }

            try
            {
                int acpiPowerReset = _acpi.DeviceSet(AsusDevice.PPT_GPUC0, 0, "ResetGpuPower");
                if (acpiPowerReset != 1)
                {
                    _logger?.LogWarning("Failed to reset GPU power via ACPI PPT_GPUC0");
                    allSucceeded = false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error resetting GPU power via ACPI");
                allSucceeded = false;
            }

            try
            {
                int acpiTempReset = _acpi.DeviceSet(AsusDevice.PPT_GPUC2, 0, "ResetGpuTempTarget");
                if (acpiTempReset != 1)
                {
                    _logger?.LogWarning("Failed to reset GPU temperature target via ACPI PPT_GPUC2");
                    allSucceeded = false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error resetting GPU temperature target via ACPI");
                allSucceeded = false;
            }

            try
            {
                _logger?.LogInformation("GPU voltage offset reset to 0% (via clock reset)");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error resetting GPU voltage offset");
            }

            _state = new GpuTuningState();

            _logger?.LogInformation("GPU tuning reset completed. Overall success: {Success}", allSucceeded);
            return allSucceeded;
        }
    }

    /// <summary>
    /// Gets the current GPU tuning state reflecting all applied settings.
    /// </summary>
    /// <returns>A <see cref="GpuTuningState"/> instance with the current configuration.</returns>
    public GpuTuningState GetState()
    {
        lock (_lock)
        {
            return _state;
        }
    }

    /// <summary>
    /// Gets the live GPU sensor state including temperature, usage, power, and clock information.
    /// This is separate from the tuning configuration state and reflects real-time hardware readings.
    /// </summary>
    /// <returns>A <see cref="GpuState"/> instance with current sensor data.</returns>
    public GpuState GetLiveState()
    {
        lock (_lock)
        {
            return _gpuControl.GetState();
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        lock (_lock)
        {
            if (_disposed) return;

            _logger?.LogDebug("Disposing GpuTuningService");
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents the current GPU tuning configuration state including clock offsets,
/// power limits, temperature targets, and voltage settings.
/// </summary>
public class GpuTuningState
{
    /// <summary>
    /// Gets or sets the core clock offset in MHz.
    /// Positive values increase the core clock, negative values decrease it.
    /// </summary>
    public int CoreClockOffset { get; set; }

    /// <summary>
    /// Gets or sets the memory clock offset in MHz.
    /// Positive values increase the memory clock, negative values decrease it.
    /// </summary>
    public int MemoryClockOffset { get; set; }

    /// <summary>
    /// Gets or sets the GPU power limit in watts.
    /// </summary>
    public int PowerLimit { get; set; }

    /// <summary>
    /// Gets or sets the GPU temperature target (junction limit) in degrees Celsius.
    /// </summary>
    public int TemperatureLimit { get; set; }

    /// <summary>
    /// Gets or sets the Dynamic Boost level in watts (0, 5, 15, or 20).
    /// Dynamic Boost allocates additional power from CPU to GPU during heavy GPU loads.
    /// </summary>
    public int DynamicBoostLevel { get; set; }

    /// <summary>
    /// Gets or sets the GPU voltage offset as a percentage.
    /// Only supported on NVIDIA GPUs. Positive values increase voltage, negative values decrease it.
    /// </summary>
    public int VoltageOffset { get; set; }
}