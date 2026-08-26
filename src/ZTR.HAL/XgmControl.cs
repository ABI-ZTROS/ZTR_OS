using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Controls XGM (External Graphics Module) features including initialization,
/// lighting control, and fan speed adjustment.
/// Uses <see cref="AsusHid"/> for HID-based communication with XGM hardware.
/// </summary>
public class XgmControl : IDisposable
{
    private readonly AsusHid _hid;
    private readonly ILogger<XgmControl>? _logger;
    private bool _disposed;

    /// <summary>
    /// Gets whether the XGM module has been initialized.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the current XGM fan speed percentage (0-100).
    /// </summary>
    public int CurrentFanSpeed { get; private set; }

    /// <summary>
    /// Gets whether XGM lighting is currently enabled.
    /// </summary>
    public bool IsLightingEnabled { get; private set; }

    /// <summary>
    /// Creates a new instance of the <see cref="XgmControl"/> class.
    /// </summary>
    /// <param name="hid">The ASUS HID interface for XGM communication.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public XgmControl(AsusHid hid, ILogger<XgmControl>? logger = null)
    {
        _hid = hid ?? throw new ArgumentNullException(nameof(hid));
        _logger = logger;
    }

    /// <summary>
    /// Initializes the XGM module, establishing communication with the hardware.
    /// </summary>
    /// <returns>True if initialization succeeded; otherwise false.</returns>
    public bool Initialize()
    {
        try
        {
            _logger?.LogInformation("Initializing XGM module");

            _hid.Initialize();

            byte[] initData = { AsusHid.XGM_REPORT_ID, 0x01, 0x00 };
            bool result = _hid.WriteXgm(initData, "XGM Initialize");

            IsInitialized = result;

            if (!result)
                _logger?.LogWarning("XGM initialization failed");
            else
                _logger?.LogInformation("XGM module initialized successfully");

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error initializing XGM module");
            IsInitialized = false;
            return false;
        }
    }

    /// <summary>
    /// Enables or disables XGM RGB lighting.
    /// </summary>
    /// <param name="enable">True to enable lighting; false to disable.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetLighting(bool enable)
    {
        try
        {
            _logger?.LogInformation("Setting XGM lighting to {State}", enable ? "enabled" : "disabled");

            byte[] data = { (byte)(enable ? 0x01 : 0x00) };
            bool result = _hid.WriteXgm(data, $"SetXgmLighting({enable})");

            if (result)
                IsLightingEnabled = enable;
            else
                _logger?.LogWarning("Failed to set XGM lighting to {State}", enable ? "enabled" : "disabled");

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting XGM lighting");
            return false;
        }
    }

    /// <summary>
    /// Sets the XGM fan speed as a percentage.
    /// </summary>
    /// <param name="speed">The fan speed percentage (0-100).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetFanSpeed(int speed)
    {
        if (speed < 0 || speed > 100)
        {
            _logger?.LogWarning("Invalid XGM fan speed: {Speed}%. Must be 0-100.", speed);
            return false;
        }

        try
        {
            _logger?.LogInformation("Setting XGM fan speed to {Speed}%", speed);

            byte[] data = { (byte)speed };
            bool result = _hid.WriteXgm(data, $"SetXgmFanSpeed({speed})");

            if (result)
                CurrentFanSpeed = speed;
            else
                _logger?.LogWarning("Failed to set XGM fan speed to {Speed}%", speed);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting XGM fan speed to {Speed}%", speed);
            return false;
        }
    }

    /// <summary>
    /// Resets XGM fan speed to automatic control.
    /// </summary>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetAutoFanSpeed()
    {
        try
        {
            _logger?.LogInformation("Setting XGM fan speed to automatic");

            byte[] data = { 0xFF };
            bool result = _hid.WriteXgm(data, "SetXgmAutoFanSpeed");

            if (result)
                CurrentFanSpeed = -1;

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting XGM fan speed to automatic");
            return false;
        }
    }

    /// <summary>
    /// Sets the XGM lighting color.
    /// </summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetLightingColor(byte r, byte g, byte b)
    {
        try
        {
            _logger?.LogInformation("Setting XGM lighting color to RGB({R}, {G}, {B})", r, g, b);

            byte[] data = { r, g, b };
            bool result = _hid.WriteXgm(data, $"SetXgmColor({r},{g},{b})");

            if (!result)
                _logger?.LogWarning("Failed to set XGM lighting color");

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting XGM lighting color");
            return false;
        }
    }

    /// <summary>
    /// Turns off XGM lighting.
    /// </summary>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool TurnOffLighting()
    {
        return SetLighting(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            IsInitialized = false;
        }
    }
}