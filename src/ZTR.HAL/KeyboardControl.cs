using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Controls keyboard backlight brightness and zones for ASUS devices.
/// Uses <see cref="AsusAcpi"/> for hardware-level communication.
/// </summary>
public class KeyboardControl : IDisposable
{
    private readonly AsusAcpi _acpi;
    private readonly ILogger<KeyboardControl>? _logger;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of the <see cref="KeyboardControl"/> class.
    /// </summary>
    /// <param name="acpi">The ASUS ACPI interface for hardware communication.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public KeyboardControl(AsusAcpi acpi, ILogger<KeyboardControl>? logger = null)
    {
        _acpi = acpi ?? throw new ArgumentNullException(nameof(acpi));
        _logger = logger;
    }

    /// <summary>
    /// Sets the keyboard backlight brightness level.
    /// </summary>
    /// <param name="level">The brightness level (0-3).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetBrightness(int level)
    {
        if (level < 0 || level > 3)
        {
            _logger?.LogWarning("Invalid keyboard brightness level: {Level}. Must be 0-3.", level);
            return false;
        }

        try
        {
            _logger?.LogInformation("Setting keyboard brightness to level {Level}", level);
            bool result = _acpi.SetKeyboardBrightness(level);

            if (!result)
                _logger?.LogWarning("Failed to set keyboard brightness to level {Level}", level);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting keyboard brightness to level {Level}", level);
            return false;
        }
    }

    /// <summary>
    /// Gets the current keyboard backlight brightness level.
    /// </summary>
    /// <returns>The brightness level (0-3), or -1 if unavailable.</returns>
    public int GetBrightness()
    {
        try
        {
            return _acpi.DeviceGet(AsusDevice.KeyboardLight);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading keyboard brightness");
            return -1;
        }
    }

    /// <summary>
    /// Sets the keyboard backlight zone for per-zone control.
    /// </summary>
    /// <param name="zone">The keyboard zone to configure.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetBacklightZone(KeyboardZone zone)
    {
        try
        {
            _logger?.LogInformation("Setting keyboard backlight zone to {Zone}", zone);
            int result = _acpi.DeviceSet(AsusDevice.KeyboardLight, (int)zone, $"SetBacklightZone({zone})");

            if (result != 1)
                _logger?.LogWarning("Failed to set backlight zone to {Zone}", zone);

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting backlight zone to {Zone}", zone);
            return false;
        }
    }

    /// <summary>
    /// Gets the current keyboard backlight zone.
    /// </summary>
    /// <returns>The current <see cref="KeyboardZone"/>, or <see cref="KeyboardZone.Zone1"/> if unavailable.</returns>
    public KeyboardZone GetBacklightZone()
    {
        try
        {
            int value = _acpi.DeviceGet(AsusDevice.KeyboardLight);
            if (Enum.IsDefined(typeof(KeyboardZone), value))
                return (KeyboardZone)value;
            return KeyboardZone.Zone1;
        }
        catch
        {
            return KeyboardZone.Zone1;
        }
    }

    /// <summary>
    /// Turns off the keyboard backlight completely.
    /// </summary>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool TurnOffBacklight()
    {
        return SetBrightness(0);
    }

    /// <summary>
    /// Sets the keyboard backlight to maximum brightness.
    /// </summary>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetMaxBrightness()
    {
        return SetBrightness(3);
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