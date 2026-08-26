using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Controls screen-related features for ASUS devices including refresh rate,
/// Overdrive, MiniLED modes, HDR, and optimal brightness.
/// Uses <see cref="AsusAcpi"/> for hardware-level communication.
/// </summary>
public class ScreenControl : IDisposable
{
    private readonly AsusAcpi _acpi;
    private readonly ILogger<ScreenControl>? _logger;
    private bool _disposed;

    private static readonly int[] _supportedRefreshRates = { 60, 120, 144, 165, 240, 300 };

    /// <summary>
    /// Creates a new instance of the <see cref="ScreenControl"/> class.
    /// </summary>
    /// <param name="acpi">The ASUS ACPI interface for hardware communication.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public ScreenControl(AsusAcpi acpi, ILogger<ScreenControl>? logger = null)
    {
        _acpi = acpi ?? throw new ArgumentNullException(nameof(acpi));
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of supported refresh rates in Hz.
    /// </summary>
    /// <returns>A read-only list of supported refresh rates.</returns>
    public IReadOnlyList<int> GetSupportedRefreshRates() => _supportedRefreshRates;

    /// <summary>
    /// Sets the screen refresh rate.
    /// </summary>
    /// <param name="rate">The refresh rate in Hz (e.g., 60, 120, 144, 240).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetRefreshRate(int rate)
    {
        if (!_supportedRefreshRates.Contains(rate))
        {
            _logger?.LogWarning("Unsupported refresh rate: {Rate}Hz", rate);
            return false;
        }

        try
        {
            _logger?.LogInformation("Setting refresh rate to {Rate}Hz", rate);
            int status = rate == 60 ? 0 : rate;
            int result = _acpi.DeviceSet(AsusDevice.ScreenFHD, status, $"SetRefreshRate({rate}Hz)");

            if (result != 1)
                _logger?.LogWarning("Failed to set refresh rate to {Rate}Hz", rate);

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting refresh rate to {Rate}Hz", rate);
            return false;
        }
    }

    /// <summary>
    /// Gets the current screen refresh rate.
    /// </summary>
    /// <returns>The refresh rate in Hz, or -1 if unavailable.</returns>
    public int GetCurrentRefreshRate()
    {
        try
        {
            int value = _acpi.DeviceGet(AsusDevice.ScreenFHD);
            if (value == -1)
                return -1;
            if (value > 0 && _supportedRefreshRates.Contains(value))
                return value;
            return value == 0 ? 60 : value;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading current refresh rate");
            return -1;
        }
    }

    /// <summary>
    /// Enables or disables Overdrive mode for the display.
    /// </summary>
    /// <param name="enable">True to enable Overdrive; false to disable.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetOverdrive(bool enable)
    {
        try
        {
            _logger?.LogInformation("Setting Overdrive to {State}", enable ? "enabled" : "disabled");
            int result = _acpi.DeviceSet(AsusDevice.ScreenOverdrive, enable ? 1 : 0, $"SetOverdrive({enable})");

            if (result != 1)
                _logger?.LogWarning("Failed to set Overdrive to {State}", enable ? "enabled" : "disabled");

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting Overdrive");
            return false;
        }
    }

    /// <summary>
    /// Gets the current Overdrive state.
    /// </summary>
    /// <returns>True if Overdrive is enabled; false otherwise.</returns>
    public bool GetOverdrive()
    {
        try
        {
            return _acpi.DeviceGet(AsusDevice.ScreenOverdrive) > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sets the MiniLED display mode.
    /// </summary>
    /// <param name="mode">The MiniLED mode to set.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetMiniLed(MiniLedMode mode)
    {
        try
        {
            _logger?.LogInformation("Setting MiniLED mode to {Mode}", mode);

            int result1 = _acpi.DeviceSet(AsusDevice.ScreenMiniled1, (int)mode, $"SetMiniLed1({mode})");
            int result2 = _acpi.DeviceSet(AsusDevice.ScreenMiniled2, (int)mode, $"SetMiniLed2({mode})");

            bool allSuccess = result1 == 1 && result2 == 1;

            if (!allSuccess)
                _logger?.LogWarning("Failed to set MiniLED mode to {Mode}", mode);

            return allSuccess;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting MiniLED mode to {Mode}", mode);
            return false;
        }
    }

    /// <summary>
    /// Gets the current MiniLED mode.
    /// </summary>
    /// <returns>The current MiniLED mode, or <see cref="MiniLedMode.Off"/> if unavailable.</returns>
    public MiniLedMode GetMiniLed()
    {
        try
        {
            int value = _acpi.DeviceGet(AsusDevice.ScreenMiniled1);
            if (Enum.IsDefined(typeof(MiniLedMode), value))
                return (MiniLedMode)value;
            return MiniLedMode.Off;
        }
        catch
        {
            return MiniLedMode.Off;
        }
    }

    /// <summary>
    /// Enables or disables HDR (High Dynamic Range) support.
    /// </summary>
    /// <param name="enable">True to enable HDR; false to disable.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetHDR(bool enable)
    {
        try
        {
            _logger?.LogInformation("Setting HDR to {State}", enable ? "enabled" : "disabled");
            int result = _acpi.DeviceSet(AsusDevice.ScreenOverdrive, enable ? 2 : 0, $"SetHDR({enable})");

            if (result != 1)
                _logger?.LogWarning("Failed to set HDR to {State}", enable ? "enabled" : "disabled");

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting HDR");
            return false;
        }
    }

    /// <summary>
    /// Gets the current HDR state.
    /// </summary>
    /// <returns>True if HDR is enabled; false otherwise.</returns>
    public bool GetHDR()
    {
        try
        {
            int value = _acpi.DeviceGet(AsusDevice.ScreenOverdrive);
            return value == 2;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Enables or disables optimal brightness control for the display.
    /// </summary>
    /// <param name="enable">True to enable optimal brightness; false to disable.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetOptimalBrightness(bool enable)
    {
        try
        {
            _logger?.LogInformation("Setting optimal brightness to {State}", enable ? "enabled" : "disabled");
            int result = _acpi.DeviceSet(AsusDevice.ScreenOptimalBrightness, enable ? 1 : 0, $"SetOptimalBrightness({enable})");

            if (result != 1)
                _logger?.LogWarning("Failed to set optimal brightness to {State}", enable ? "enabled" : "disabled");

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting optimal brightness");
            return false;
        }
    }

    /// <summary>
    /// Gets the current optimal brightness state.
    /// </summary>
    /// <returns>True if optimal brightness is enabled; false otherwise.</returns>
    public bool GetOptimalBrightness()
    {
        try
        {
            return _acpi.DeviceGet(AsusDevice.ScreenOptimalBrightness) > 0;
        }
        catch
        {
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