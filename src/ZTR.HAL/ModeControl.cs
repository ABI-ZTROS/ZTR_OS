using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Central controller for ASUS performance mode switching, fan curve management,
/// and power limit control. Coordinates between <see cref="AsusAcpi"/>,
/// <see cref="FanCurveCalculator"/>, and <see cref="PowerLimitManager"/>.
/// </summary>
public class ModeControl : IDisposable
{
    private readonly AsusAcpi _acpi;
    private readonly PowerLimitManager _powerManager;
    private readonly ILogger<ModeControl>? _logger;
    private bool _disposed;

    private AsusMode _currentMode;
    private FanCurvePoint[] _cpuFanCurve = Array.Empty<FanCurvePoint>();
    private FanCurvePoint[] _gpuFanCurve = Array.Empty<FanCurvePoint>();
    private FanCurvePoint[] _midFanCurve = Array.Empty<FanCurvePoint>();
    private int _cpuTempLimit;
    private int _spl;
    private int _sppt;
    private int _fppt;

    /// <summary>
    /// Gets the current performance mode.
    /// </summary>
    public AsusMode CurrentMode => _currentMode;

    /// <summary>
    /// Gets the CPU temperature wall limit in degrees Celsius.
    /// </summary>
    public int CpuTempLimit => _cpuTempLimit;

    /// <summary>
    /// Creates a new instance of the <see cref="ModeControl"/> class.
    /// </summary>
    /// <param name="acpi">The ASUS ACPI interface for hardware communication.</param>
    /// <param name="powerManager">The power limit manager for power settings.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public ModeControl(AsusAcpi acpi, PowerLimitManager powerManager, ILogger<ModeControl>? logger = null)
    {
        _acpi = acpi;
        _powerManager = powerManager;
        _logger = logger;
        _currentMode = AsusMode.PerformanceBalanced;
    }

    /// <summary>
    /// Switches the performance mode and applies associated fan curves and power settings.
    /// </summary>
    /// <param name="mode">The performance mode to switch to.</param>
    /// <returns>True if the mode switch succeeded; otherwise false.</returns>
    public bool SetMode(AsusMode mode)
    {
        try
        {
            _logger?.LogInformation("Switching performance mode from {CurrentMode} to {NewMode}", _currentMode, mode);

            bool result = _acpi.SetPerformanceMode(mode);
            if (!result)
            {
                _logger?.LogWarning("Failed to set performance mode to {Mode} via ACPI", mode);
                return false;
            }

            _currentMode = mode;

            ApplyModeDefaults(mode);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error switching performance mode to {Mode}", mode);
            return false;
        }
    }

    /// <summary>
    /// Gets the current performance mode from the hardware.
    /// </summary>
    /// <returns>The current <see cref="AsusMode"/>.</returns>
    public AsusMode GetCurrentMode()
    {
        try
        {
            int modeValue = _acpi.GetPerformanceMode();
            if (modeValue >= 0 && Enum.IsDefined(typeof(AsusMode), modeValue))
            {
                _currentMode = (AsusMode)modeValue;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading current performance mode");
        }

        return _currentMode;
    }

    /// <summary>
    /// Sets the CPU fan curve using the specified temperature/speed points.
    /// </summary>
    /// <param name="curve">An array of <see cref="FanCurvePoint"/> defining the fan curve.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetCpuFanCurve(FanCurvePoint[] curve)
    {
        ArgumentNullException.ThrowIfNull(curve);
        try
        {
            byte[] bytes = FanCurveCalculator.CurveToBytes(curve);

            _logger?.LogInformation("Setting CPU fan curve with {PointCount} points", curve.Length);

            bool result = _acpi.SetCpuFanCurve(bytes);
            if (result)
            {
                _cpuFanCurve = curve;
            }
            else
            {
                _logger?.LogWarning("Failed to set CPU fan curve");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting CPU fan curve");
            return false;
        }
    }

    /// <summary>
    /// Sets the GPU fan curve using the specified temperature/speed points.
    /// </summary>
    /// <param name="curve">An array of <see cref="FanCurvePoint"/> defining the fan curve.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetGpuFanCurve(FanCurvePoint[] curve)
    {
        ArgumentNullException.ThrowIfNull(curve);
        try
        {
            byte[] bytes = FanCurveCalculator.CurveToBytes(curve);

            _logger?.LogInformation("Setting GPU fan curve with {PointCount} points", curve.Length);

            bool result = _acpi.SetGpuFanCurve(bytes);
            if (result)
            {
                _gpuFanCurve = curve;
            }
            else
            {
                _logger?.LogWarning("Failed to set GPU fan curve");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting GPU fan curve");
            return false;
        }
    }

    /// <summary>
    /// Sets the Mid fan curve using the specified temperature/speed points.
    /// </summary>
    /// <param name="curve">An array of <see cref="FanCurvePoint"/> defining the fan curve.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetMidFanCurve(FanCurvePoint[] curve)
    {
        ArgumentNullException.ThrowIfNull(curve);
        try
        {
            byte[] bytes = FanCurveCalculator.CurveToBytes(curve);

            _logger?.LogInformation("Setting Mid fan curve with {PointCount} points", curve.Length);

            bool result = _acpi.DeviceSet(AsusDevice.DevsMidFanCurve, bytes, "SetMidFanCurve");
            if (result)
            {
                _midFanCurve = curve;
            }
            else
            {
                _logger?.LogWarning("Failed to set Mid fan curve");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting Mid fan curve");
            return false;
        }
    }

    /// <summary>
    /// Gets the current fan curve for the specified fan type.
    /// </summary>
    /// <param name="fan">The fan type to retrieve the curve for.</param>
    /// <returns>An array of <see cref="FanCurvePoint"/> representing the current fan curve.</returns>
    public FanCurvePoint[] GetFanCurve(AsusFan fan)
    {
        try
        {
            AsusDevice deviceId = fan switch
            {
                AsusFan.CPU => AsusDevice.DevsCPUFanCurve,
                AsusFan.GPU => AsusDevice.DevsGPUFanCurve,
                AsusFan.Mid => AsusDevice.DevsMidFanCurve,
                _ => AsusDevice.DevsCPUFanCurve
            };

            byte[] buffer = _acpi.DeviceGetBuffer(deviceId, 0);
            if (buffer.Length >= FanCurveCalculator.CurveByteSize)
            {
                return FanCurveCalculator.BytesToCurve(buffer);
            }

            _logger?.LogWarning("Failed to read fan curve for {Fan}, returning cached curve", fan);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading fan curve for {Fan}", fan);
        }

        return fan switch
        {
            AsusFan.CPU => _cpuFanCurve,
            AsusFan.GPU => _gpuFanCurve,
            AsusFan.Mid => _midFanCurve,
            _ => Array.Empty<FanCurvePoint>()
        };
    }

    /// <summary>
    /// Sets all three power limits: SPL, sPPT, and fPPT.
    /// </summary>
    /// <param name="spl">Short Power Limit in watts.</param>
    /// <param name="sppt">Short Power Peak Throttling in watts.</param>
    /// <param name="fppt">Fast Power Peak Throttling in watts.</param>
    /// <returns>True if all operations succeeded; otherwise false.</returns>
    public bool SetPowerLimits(int spl, int sppt, int fppt)
    {
        try
        {
            _logger?.LogInformation("Setting power limits - SPL: {Spl}W, sPPT: {Sppt}W, fPPT: {Fppt}W", spl, sppt, fppt);

            bool result = _powerManager.SetAllPowerLimits(spl, sppt, fppt);
            if (result)
            {
                _spl = spl;
                _sppt = sppt;
                _fppt = fppt;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting power limits");
            return false;
        }
    }

    /// <summary>
    /// Gets the current power limits configuration.
    /// </summary>
    /// <returns>A tuple containing (spl, sppt, fppt) values in watts.</returns>
    public (int spl, int sppt, int fppt) GetPowerLimits()
    {
        return (_spl, _sppt, _fppt);
    }

    /// <summary>
    /// Sets the CPU temperature wall (thermal limit).
    /// </summary>
    /// <param name="temp">The temperature limit in degrees Celsius.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetCpuTempLimit(int temp)
    {
        try
        {
            temp = Math.Clamp(temp, 60, 110);
            _logger?.LogInformation("Setting CPU temperature limit to {Temp}C", temp);

            bool result = _acpi.DeviceSet(AsusDevice.PPT_APUA0, temp, $"SetCpuTempLimit({temp}C)");
            if (result)
            {
                _cpuTempLimit = temp;
            }
            else
            {
                _logger?.LogWarning("Failed to set CPU temperature limit to {Temp}C", temp);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting CPU temperature limit to {Temp}C", temp);
            return false;
        }
    }

    /// <summary>
    /// Auto-applies all current mode settings including fan curves and power limits.
    /// This should be called after system startup or when settings need to be re-applied.
    /// </summary>
    /// <returns>True if all settings were applied successfully; otherwise false.</returns>
    public bool AutoApplySettings()
    {
        try
        {
            _logger?.LogInformation("Auto-applying settings for mode {Mode}", _currentMode);

            bool modeResult = _acpi.SetPerformanceMode(_currentMode);
            bool fanCpuResult = _cpuFanCurve.Length > 0 ? _acpi.SetCpuFanCurve(FanCurveCalculator.CurveToBytes(_cpuFanCurve)) : true;
            bool fanGpuResult = _gpuFanCurve.Length > 0 ? _acpi.SetGpuFanCurve(FanCurveCalculator.CurveToBytes(_gpuFanCurve)) : true;
            bool fanMidResult = _midFanCurve.Length > 0 ? _acpi.DeviceSet(AsusDevice.DevsMidFanCurve, FanCurveCalculator.CurveToBytes(_midFanCurve), "AutoApplyMidFan") : true;

            bool powerResult = true;
            if (_spl > 0 || _sppt > 0 || _fppt > 0)
            {
                powerResult = _powerManager.SetAllPowerLimits(_spl, _sppt, _fppt);
            }

            bool tempResult = true;
            if (_cpuTempLimit > 0)
            {
                tempResult = _acpi.DeviceSet(AsusDevice.PPT_APUA0, _cpuTempLimit, "AutoApplyCpuTempLimit");
            }

            bool allSuccess = modeResult && fanCpuResult && fanGpuResult && fanMidResult && powerResult && tempResult;

            if (allSuccess)
            {
                _logger?.LogInformation("All settings applied successfully");
            }
            else
            {
                _logger?.LogWarning("Some settings failed to apply - mode:{Mode}, CPU fan:{Cpu}, GPU fan:{Gpu}, Mid fan:{Mid}, power:{Power}, temp:{Temp}",
                    modeResult, fanCpuResult, fanGpuResult, fanMidResult, powerResult, tempResult);
            }

            return allSuccess;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during auto-apply settings");
            return false;
        }
    }

    /// <summary>
    /// Applies default fan curves and power settings for the specified mode.
    /// </summary>
    /// <param name="mode">The mode to apply defaults for.</param>
    private void ApplyModeDefaults(AsusMode mode)
    {
        try
        {
            if (mode != AsusMode.PerformanceManual)
            {
                var cpuCurve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, mode);
                var gpuCurve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.GPU, mode);
                var midCurve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.Mid, mode);

                SetCpuFanCurve(cpuCurve);
                SetGpuFanCurve(gpuCurve);
                SetMidFanCurve(midCurve);
            }

            _powerManager.ApplyModePowerDefaults(mode);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error applying mode defaults for {Mode}", mode);
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