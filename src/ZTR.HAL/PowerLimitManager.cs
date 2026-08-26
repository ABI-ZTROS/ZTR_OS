using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Manages CPU and GPU power limits for ASUS devices, including Short Power Limit (SPL),
/// Short Power Peak Throttling (sPPT), and Fast Power Peak Throttling (fPPT).
/// Provides per-mode power configurations and Dynamic Boost settings.
/// </summary>
public class PowerLimitManager : IDisposable
{
    private readonly AsusAcpi _acpi;
    private readonly ILogger<PowerLimitManager>? _logger;
    private bool _disposed;

    private int _currentSpl;
    private int _currentSppt;
    private int _currentFppt;
    private int _dynamicBoostLevel;
    private AsusMode _currentMode;

    private static readonly Dictionary<AsusMode, (int spl, int sppt, int fppt)> _modePowerDefaults = new()
    {
        [AsusMode.PerformanceSilent] = (15, 10, 5),
        [AsusMode.PerformanceBalanced] = (35, 25, 15),
        [AsusMode.PerformanceTurbo] = (45, 35, 20),
        [AsusMode.PerformanceFullSpeed] = (55, 45, 25),
        [AsusMode.PerformanceManual] = (35, 25, 15)
    };

    /// <summary>
    /// Creates a new instance of the <see cref="PowerLimitManager"/> class.
    /// </summary>
    /// <param name="acpi">The ASUS ACPI interface for hardware communication.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public PowerLimitManager(AsusAcpi acpi, ILogger<PowerLimitManager>? logger = null)
    {
        _acpi = acpi;
        _logger = logger;
    }

    /// <summary>
    /// Sets the Short Power Limit (SPL) in watts.
    /// </summary>
    /// <param name="watts">The power limit in watts.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetSPL(int watts)
    {
        try
        {
            watts = Math.Clamp(watts, 0, 250);
            _logger?.LogInformation("Setting SPL to {Watts}W", watts);

            int result = _acpi.DeviceSet(AsusDevice.PPT_APUA0, watts, $"SetSPL({watts}W)");
            if (result == 1)
            {
                _currentSpl = watts;
            }
            else
            {
                _logger?.LogWarning("Failed to set SPL to {Watts}W", watts);
            }

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting SPL to {Watts}W", watts);
            return false;
        }
    }

    /// <summary>
    /// Sets the Short Power Peak Throttling (sPPT) in watts.
    /// </summary>
    /// <param name="watts">The peak throttling limit in watts.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetSPPT(int watts)
    {
        try
        {
            watts = Math.Clamp(watts, 0, 250);
            _logger?.LogInformation("Setting sPPT to {Watts}W", watts);

            int result = _acpi.DeviceSet(AsusDevice.PPT_APUA3, watts, $"SetSPPT({watts}W)");
            if (result == 1)
            {
                _currentSppt = watts;
            }
            else
            {
                _logger?.LogWarning("Failed to set sPPT to {Watts}W", watts);
            }

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting sPPT to {Watts}W", watts);
            return false;
        }
    }

    /// <summary>
    /// Sets the Fast Power Peak Throttling (fPPT) in watts.
    /// </summary>
    /// <param name="watts">The fast peak throttling limit in watts.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetFPPT(int watts)
    {
        try
        {
            watts = Math.Clamp(watts, 0, 250);
            _logger?.LogInformation("Setting fPPT to {Watts}W", watts);

            int result = _acpi.DeviceSet(AsusDevice.PPT_APUC1, watts, $"SetFPPT({watts}W)");
            if (result == 1)
            {
                _currentFppt = watts;
            }
            else
            {
                _logger?.LogWarning("Failed to set fPPT to {Watts}W", watts);
            }

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting fPPT to {Watts}W", watts);
            return false;
        }
    }

    /// <summary>
    /// Sets all three power limits (SPL, sPPT, fPPT) simultaneously.
    /// </summary>
    /// <param name="spl">Short Power Limit in watts.</param>
    /// <param name="sppt">Short Power Peak Throttling in watts.</param>
    /// <param name="fppt">Fast Power Peak Throttling in watts.</param>
    /// <returns>True if all operations succeeded; otherwise false.</returns>
    public bool SetAllPowerLimits(int spl, int sppt, int fppt)
    {
        bool splResult = SetSPL(spl);
        bool spptResult = SetSPPT(sppt);
        bool fpptResult = SetFPPT(fppt);

        return splResult && spptResult && fpptResult;
    }

    /// <summary>
    /// Gets the current power configuration including SPL, sPPT, fPPT, and Dynamic Boost level.
    /// </summary>
    /// <returns>A <see cref="PowerState"/> object with the current power configuration.</returns>
    public PowerState GetPowerState()
    {
        return new PowerState
        {
            SPL = _currentSpl,
            SPPT = _currentSppt,
            FPPT = _currentFppt,
            DynamicBoostLevel = _dynamicBoostLevel,
            Mode = _currentMode
        };
    }

    /// <summary>
    /// Sets the Dynamic Boost level (5, 15, or 20 watts).
    /// </summary>
    /// <param name="level">The Dynamic Boost level in watts (5, 15, or 20).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetDynamicBoost(int level)
    {
        try
        {
            int normalizedLevel = level switch
            {
                5 => 1,
                15 => 2,
                20 => 3,
                _ => 0
            };

            _logger?.LogInformation("Setting Dynamic Boost to {Level}W (value: {Value})", level, normalizedLevel);

            int result = _acpi.DeviceSet(AsusDevice.PPT_APUA3, normalizedLevel, $"SetDynamicBoost({level}W)");
            if (result == 1)
            {
                _dynamicBoostLevel = level;
            }
            else
            {
                _logger?.LogWarning("Failed to set Dynamic Boost to {Level}W", level);
            }

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting Dynamic Boost to {Level}W", level);
            return false;
        }
    }

    /// <summary>
    /// Applies the default power configuration for the specified performance mode.
    /// </summary>
    /// <param name="mode">The performance mode to apply power settings for.</param>
    /// <returns>True if all power limits were successfully applied; otherwise false.</returns>
    public bool ApplyModePowerDefaults(AsusMode mode)
    {
        if (_modePowerDefaults.TryGetValue(mode, out var defaults))
        {
            _currentMode = mode;
            return SetAllPowerLimits(defaults.spl, defaults.sppt, defaults.fppt);
        }

        _logger?.LogWarning("No default power configuration found for mode {Mode}", mode);
        return false;
    }

    /// <summary>
    /// Gets the default power limits for a specified performance mode.
    /// </summary>
    /// <param name="mode">The performance mode.</param>
    /// <returns>A tuple containing (spl, sppt, fppt) defaults, or null if mode is unknown.</returns>
    public (int spl, int sppt, int fppt)? GetModePowerDefaults(AsusMode mode)
    {
        return _modePowerDefaults.TryGetValue(mode, out var defaults)
            ? defaults
            : null;
    }

    /// <summary>
    /// Resets all power limits to their default values for the current mode.
    /// </summary>
    /// <returns>True if the reset succeeded; otherwise false.</returns>
    public bool ResetToDefaults()
    {
        return ApplyModePowerDefaults(_currentMode);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents the current power configuration state.
/// </summary>
public class PowerState
{
    /// <summary>
    /// Gets or sets the Short Power Limit in watts.
    /// </summary>
    public int SPL { get; set; }

    /// <summary>
    /// Gets or sets the Short Power Peak Throttling in watts.
    /// </summary>
    public int SPPT { get; set; }

    /// <summary>
    /// Gets or sets the Fast Power Peak Throttling in watts.
    /// </summary>
    public int FPPT { get; set; }

    /// <summary>
    /// Gets or sets the Dynamic Boost level in watts.
    /// </summary>
    public int DynamicBoostLevel { get; set; }

    /// <summary>
    /// Gets or sets the performance mode associated with this power state.
    /// </summary>
    public AsusMode Mode { get; set; }
}