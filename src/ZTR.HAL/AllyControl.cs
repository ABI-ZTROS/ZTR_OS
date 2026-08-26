using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Controls ASUS Ally handheld device features including controller input modes,
/// FPS limiting, auto TDP, vibration, and key mapping.
/// Uses <see cref="AsusAcpi"/> for hardware-level communication.
/// </summary>
public class AllyControl : IDisposable
{
    private readonly AsusAcpi _acpi;
    private readonly AsusHid _hid;
    private readonly ILogger<AllyControl>? _logger;
    private bool _disposed;

    private static readonly int[] _supportedFpsLimits = { 30, 40, 45, 50, 60, 75, 90, 120, 240 };

    /// <summary>
    /// Creates a new instance of the <see cref="AllyControl"/> class.
    /// </summary>
    /// <param name="acpi">The ASUS ACPI interface for hardware communication.</param>
    /// <param name="hid">The ASUS HID interface for controller input communication.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public AllyControl(AsusAcpi acpi, AsusHid hid, ILogger<AllyControl>? logger = null)
    {
        _acpi = acpi ?? throw new ArgumentNullException(nameof(acpi));
        _hid = hid ?? throw new ArgumentNullException(nameof(hid));
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of supported FPS limit values.
    /// </summary>
    /// <returns>A read-only list of supported FPS limits.</returns>
    public IReadOnlyList<int> GetSupportedFpsLimits() => _supportedFpsLimits;

    /// <summary>
    /// Sets the controller input mode (Auto, Gamepad, WASD, or Mouse).
    /// </summary>
    /// <param name="mode">The controller mode to set.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetControllerMode(ControllerMode mode)
    {
        try
        {
            _logger?.LogInformation("Setting controller mode to {Mode}", mode);

            byte[] data = { (byte)mode };
            bool result = _hid.WriteInput(data, $"SetControllerMode({mode})");

            if (!result)
                _logger?.LogWarning("Failed to set controller mode to {Mode}", mode);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting controller mode to {Mode}", mode);
            return false;
        }
    }

    /// <summary>
    /// Gets the current controller mode.
    /// </summary>
    /// <returns>The current <see cref="ControllerMode"/>, or <see cref="ControllerMode.Auto"/> if unavailable.</returns>
    public ControllerMode GetControllerMode()
    {
        try
        {
            byte[]? data = _hid.ReadFeature(AsusHid.INPUT_ID, 1);
            if (data != null && data.Length >= 1 && Enum.IsDefined(typeof(ControllerMode), data[0]))
                return (ControllerMode)data[0];
            return ControllerMode.Auto;
        }
        catch
        {
            return ControllerMode.Auto;
        }
    }

    /// <summary>
    /// Sets the FPS (frames per second) limit for the display.
    /// </summary>
    /// <param name="fps">The FPS limit value (30, 40, 45, 50, 60, 75, 90, 120, 240).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetFpsLimit(int fps)
    {
        if (!_supportedFpsLimits.Contains(fps))
        {
            _logger?.LogWarning("Unsupported FPS limit: {Fps}. Supported: {Supported}",
                fps, string.Join(", ", _supportedFpsLimits));
            return false;
        }

        try
        {
            _logger?.LogInformation("Setting FPS limit to {Fps}", fps);
            int result = _acpi.DeviceSet(AsusDevice.ScreenFHD, fps, $"SetFpsLimit({fps})");

            if (result != 1)
                _logger?.LogWarning("Failed to set FPS limit to {Fps}", fps);

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting FPS limit to {Fps}", fps);
            return false;
        }
    }

    /// <summary>
    /// Gets the current FPS limit setting.
    /// </summary>
    /// <returns>The FPS limit value, or -1 if unavailable.</returns>
    public int GetFpsLimit()
    {
        try
        {
            return _acpi.DeviceGet(AsusDevice.ScreenFHD);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading FPS limit");
            return -1;
        }
    }

    /// <summary>
    /// Sets the auto TDP (Thermal Design Power) for the APU.
    /// </summary>
    /// <param name="tdp">The TDP value in watts (6-25).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetAutoTDP(int tdp)
    {
        if (tdp < 6 || tdp > 25)
        {
            _logger?.LogWarning("Auto TDP must be between 6-25W, got {Tdp}W", tdp);
            return false;
        }

        try
        {
            _logger?.LogInformation("Setting auto TDP to {Tdp}W", tdp);
            int result = _acpi.DeviceSet(AsusDevice.PPT_APUA0, tdp, $"SetAutoTDP({tdp}W)");

            if (result != 1)
                _logger?.LogWarning("Failed to set auto TDP to {Tdp}W", tdp);

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting auto TDP to {Tdp}W", tdp);
            return false;
        }
    }

    /// <summary>
    /// Gets the current auto TDP value.
    /// </summary>
    /// <returns>The TDP value in watts, or -1 if unavailable.</returns>
    public int GetAutoTDP()
    {
        try
        {
            return _acpi.DeviceGet(AsusDevice.PPT_APUA0);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading auto TDP");
            return -1;
        }
    }

    /// <summary>
    /// Sets the controller vibration intensity.
    /// </summary>
    /// <param name="intensity">The vibration intensity (0-100).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetVibration(int intensity)
    {
        if (intensity < 0 || intensity > 100)
        {
            _logger?.LogWarning("Vibration intensity must be 0-100, got {Intensity}", intensity);
            return false;
        }

        try
        {
            _logger?.LogInformation("Setting vibration intensity to {Intensity}%", intensity);

            byte[] data = { (byte)(intensity * 255 / 100) };
            bool result = _hid.WriteInput(data, $"SetVibration({intensity})");

            if (!result)
                _logger?.LogWarning("Failed to set vibration intensity to {Intensity}%", intensity);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting vibration intensity to {Intensity}%", intensity);
            return false;
        }
    }

    /// <summary>
    /// Gets the current vibration intensity.
    /// </summary>
    /// <returns>The vibration intensity (0-100), or -1 if unavailable.</returns>
    public int GetVibration()
    {
        try
        {
            byte[]? data = _hid.ReadFeature(AsusHid.INPUT_ID, 1);
            if (data != null && data.Length >= 1)
                return data[0] * 100 / 255;
            return -1;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// Sets a custom key mapping for the controller.
    /// </summary>
    /// <param name="buttonId">The button identifier (0-15).</param>
    /// <param name="keyCode">The key code to map to the button.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetKeyMapping(int buttonId, int keyCode)
    {
        if (buttonId < 0 || buttonId > 15)
        {
            _logger?.LogWarning("Button ID must be 0-15, got {ButtonId}", buttonId);
            return false;
        }

        if (keyCode < 0 || keyCode > 255)
        {
            _logger?.LogWarning("Key code must be 0-255, got {KeyCode}", keyCode);
            return false;
        }

        try
        {
            _logger?.LogInformation("Mapping button {ButtonId} to key code {KeyCode}", buttonId, keyCode);

            byte[] data = { (byte)buttonId, (byte)keyCode };
            bool result = _hid.WriteInput(data, $"SetKeyMapping({buttonId}->{keyCode})");

            if (!result)
                _logger?.LogWarning("Failed to map button {ButtonId} to key code {KeyCode}", buttonId, keyCode);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error mapping button {ButtonId} to key code {KeyCode}", buttonId, keyCode);
            return false;
        }
    }

    /// <summary>
    /// Resets all key mappings to defaults.
    /// </summary>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool ResetKeyMappings()
    {
        try
        {
            _logger?.LogInformation("Resetting all key mappings to defaults");
            byte[] data = { 0xFF };
            return _hid.WriteInput(data, "ResetKeyMappings");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error resetting key mappings");
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