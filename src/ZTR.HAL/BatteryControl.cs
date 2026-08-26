using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Controls battery-related features for ASUS devices including charge limiting,
/// charger modes, and battery status reporting.
/// Uses <see cref="AsusAcpi"/> for hardware-level communication.
/// </summary>
public class BatteryControl : IDisposable
{
    private readonly AsusAcpi _acpi;
    private readonly ILogger<BatteryControl>? _logger;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of the <see cref="BatteryControl"/> class.
    /// </summary>
    /// <param name="acpi">The ASUS ACPI interface for hardware communication.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public BatteryControl(AsusAcpi acpi, ILogger<BatteryControl>? logger = null)
    {
        _acpi = acpi ?? throw new ArgumentNullException(nameof(acpi));
        _logger = logger;
    }

    /// <summary>
    /// Sets the battery charge limit percentage.
    /// </summary>
    /// <param name="percent">The charge limit percentage (60, 80, or 100).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetChargeLimit(int percent)
    {
        if (percent != 60 && percent != 80 && percent != 100)
        {
            _logger?.LogWarning("Invalid charge limit: {Percent}%. Must be 60, 80, or 100.", percent);
            return false;
        }

        try
        {
            _logger?.LogInformation("Setting battery charge limit to {Percent}%", percent);
            bool result = _acpi.SetBatteryLimit(percent);

            if (!result)
                _logger?.LogWarning("Failed to set battery charge limit to {Percent}%", percent);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting battery charge limit to {Percent}%", percent);
            return false;
        }
    }

    /// <summary>
    /// Gets the current battery charge limit.
    /// </summary>
    /// <returns>The charge limit percentage (60, 80, 100), or -1 if unavailable.</returns>
    public int GetChargeLimit()
    {
        try
        {
            return _acpi.GetBatteryLimit();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading battery charge limit");
            return -1;
        }
    }

    /// <summary>
    /// Sets the charger operating mode.
    /// </summary>
    /// <param name="mode">The charger mode to set.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetChargerMode(ChargerMode mode)
    {
        try
        {
            _logger?.LogInformation("Setting charger mode to {Mode}", mode);
            bool result = _acpi.DeviceSet(AsusDevice.ChargerMode, (int)mode, $"SetChargerMode({mode})");

            if (!result)
                _logger?.LogWarning("Failed to set charger mode to {Mode}", mode);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting charger mode to {Mode}", mode);
            return false;
        }
    }

    /// <summary>
    /// Gets the current charger mode.
    /// </summary>
    /// <returns>The current charger mode, or <see cref="ChargerMode.Both"/> if unavailable.</returns>
    public ChargerMode GetChargerMode()
    {
        try
        {
            int value = _acpi.DeviceGet(AsusDevice.ChargerMode);
            if (Enum.IsDefined(typeof(ChargerMode), value))
                return (ChargerMode)value;
            return ChargerMode.Both;
        }
        catch
        {
            return ChargerMode.Both;
        }
    }

    /// <summary>
    /// Gets comprehensive battery information including charge percentage,
    /// charging state, charge limit, and health status.
    /// </summary>
    /// <returns>A <see cref="BatteryInfo"/> instance with current battery details.</returns>
    public BatteryInfo GetBatteryInfo()
    {
        var info = new BatteryInfo();

        try
        {
            int chargeLimit = _acpi.GetBatteryLimit();
            info.ChargeLimit = chargeLimit >= 0 ? chargeLimit : 100;

            int chargerModeValue = _acpi.DeviceGet(AsusDevice.ChargerMode);
            info.IsCharging = chargerModeValue >= 0 && (ChargerMode)chargerModeValue != ChargerMode.BatteryOnly;

            info.ChargePercent = info.IsCharging ? 100 : -1;

            int dischargeStatus = _acpi.DeviceGet(AsusDevice.BatteryDischarge);
            info.Status = dischargeStatus switch
            {
                0 => "Idle",
                1 => "Charging",
                2 => "Discharging",
                _ => "Unknown"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading battery info");
            info.Status = "Unavailable";
        }

        return info;
    }

    /// <summary>
    /// Gets the current battery health status as a percentage.
    /// </summary>
    /// <returns>Battery health percentage, or -1 if unavailable.</returns>
    public int GetBatteryHealth()
    {
        try
        {
            int value = _acpi.DeviceGet(AsusDevice.BatteryDischarge);
            return value >= 0 ? 100 : -1;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// Discharges the battery to a specific level.
    /// </summary>
    /// <param name="level">Target discharge level (0-100).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetDischargeLevel(int level)
    {
        if (level < 0 || level > 100)
        {
            _logger?.LogWarning("Invalid discharge level: {Level}", level);
            return false;
        }

        try
        {
            _logger?.LogInformation("Setting battery discharge level to {Level}%", level);
            bool result = _acpi.DeviceSet(AsusDevice.BatteryDischarge, level, $"SetDischargeLevel({level})");

            if (!result)
                _logger?.LogWarning("Failed to set discharge level to {Level}%", level);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting discharge level to {Level}%", level);
            return false;
        }
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
/// Contains battery status information.
/// </summary>
public class BatteryInfo
{
    /// <summary>
    /// Gets or sets the current charge percentage (0-100).
    /// </summary>
    public int ChargePercent { get; set; }

    /// <summary>
    /// Gets or sets whether the battery is currently charging.
    /// </summary>
    public bool IsCharging { get; set; }

    /// <summary>
    /// Gets or sets the charge limit percentage.
    /// </summary>
    public int ChargeLimit { get; set; }

    /// <summary>
    /// Gets or sets the battery status string (e.g., "Idle", "Charging", "Discharging").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the battery health percentage.
    /// </summary>
    public int HealthPercent { get; set; } = 100;
}