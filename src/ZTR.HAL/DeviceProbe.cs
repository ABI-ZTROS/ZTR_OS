using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Probes for ASUS hardware capabilities by combining WMI and ACPI queries.
/// Detects device model, BIOS version, and supported features.
/// </summary>
public class DeviceProbe
{
    private readonly WmiHelper _wmiHelper;
    private readonly AsusAcpi _acpi;
    private readonly ILogger<DeviceProbe>? _logger;

    /// <summary>
    /// Creates a new instance of the <see cref="DeviceProbe"/> class.
    /// </summary>
    /// <param name="wmiHelper">The WMI helper for hardware queries.</param>
    /// <param name="acpi">The ACPI interface for device capability detection.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public DeviceProbe(WmiHelper wmiHelper, AsusAcpi acpi, ILogger<DeviceProbe>? logger = null)
    {
        _wmiHelper = wmiHelper;
        _acpi = acpi;
        _logger = logger;
    }

    /// <summary>
    /// Probes the system for ASUS hardware and returns a <see cref="DeviceInfo"/> describing its capabilities.
    /// </summary>
    /// <returns>A <see cref="DeviceInfo"/> instance describing the detected hardware.</returns>
    public DeviceInfo Probe()
    {
        _logger?.LogInformation("Starting device probe...");

        var info = new DeviceInfo();

        PopulateWmiInfo(info);
        PopulateAcpiCapabilities(info);

        _logger?.LogInformation("Device probe complete. Model={Model}, Type={Type}, Features={Features}",
            info.Model, info.Type, string.Join(", ", info.SupportedFeatures));

        return info;
    }

    private void PopulateWmiInfo(DeviceInfo info)
    {
        try
        {
            info.Model = _wmiHelper.GetHardwareModel();
            info.BiosVersion = _wmiHelper.GetBiosVersion();
            info.CpuModel = _wmiHelper.GetCpuInfo();
            info.GpuModel = _wmiHelper.GetGpuInfo();
            info.Manufacturer = _wmiHelper.GetManufacturer();
            info.Type = _wmiHelper.GetDeviceType();

            _logger?.LogDebug("WMI info populated: Model={Model}, Bios={Bios}, CPU={Cpu}, GPU={Gpu}",
                info.Model, info.BiosVersion, info.CpuModel, info.GpuModel);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to populate WMI info");
        }
    }

    private void PopulateAcpiCapabilities(DeviceInfo info)
    {
        info.IsAtkAcpiAvailable = _acpi.IsAvailable;

        if (!_acpi.IsAvailable)
        {
            _logger?.LogWarning("ATKACPI is not available. Skipping ACPI capability detection.");
            info.SupportedFeatures = Array.Empty<string>();
            return;
        }

        var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        DetectPerformanceModeSupport(info, features);
        DetectFanControlSupport(info, features);
        DetectGpuModeSupport(info, features);
        DetectBatteryLimitSupport(info, features);
        DetectAuraSupport(features);

        info.SupportedFeatures = features.ToList().AsReadOnly();

        _logger?.LogDebug("ACPI capabilities: Features={Features}", string.Join(", ", features));
    }

    private void DetectPerformanceModeSupport(DeviceInfo info, HashSet<string> features)
    {
        try
        {
            int mode = _acpi.GetPerformanceMode();
            if (mode >= 0)
            {
                info.SupportsPerformanceModes = true;
                features.Add("PerformanceModes");
                _logger?.LogDebug("Performance mode support detected: CurrentMode={Mode}", mode);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to detect performance mode support");
        }
    }

    private void DetectFanControlSupport(DeviceInfo info, HashSet<string> features)
    {
        try
        {
            int cpuTemp = _acpi.GetCpuTemperature();
            if (cpuTemp >= 0)
            {
                info.SupportsFanControl = true;
                features.Add("FanControl");
                _logger?.LogDebug("CPU fan/temp support detected: Temperature={Temp}", cpuTemp);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to detect fan control support");
        }

        try
        {
            int gpuTemp = _acpi.GetGpuTemperature();
            if (gpuTemp >= 0)
            {
                info.GpuFanCount = 1;
                _logger?.LogDebug("GPU fan support detected: Temperature={Temp}", gpuTemp);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to detect GPU fan support");
        }

        info.CpuFanCount = 1;
    }

    private void DetectGpuModeSupport(DeviceInfo info, HashSet<string> features)
    {
        try
        {
            int gpuMode = _acpi.GetGPUMode();
            if (gpuMode >= 0)
            {
                info.SupportsGpuModes = true;
                features.Add("GPUModes");
                _logger?.LogDebug("GPU mode support detected: CurrentMode={Mode}", gpuMode);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to detect GPU mode support");
        }
    }

    private void DetectBatteryLimitSupport(DeviceInfo info, HashSet<string> features)
    {
        try
        {
            int limit = _acpi.GetBatteryLimit();
            if (limit >= 0)
            {
                info.SupportsBatteryLimit = true;
                features.Add("BatteryLimit");
                _logger?.LogDebug("Battery limit support detected: CurrentLimit={Limit}", limit);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to detect battery limit support");
        }
    }

    private void DetectAuraSupport(HashSet<string> features)
    {
        try
        {
            int keyboardLight = _acpi.DeviceGet(AsusDevice.KeyboardLight);
            if (keyboardLight >= 0)
            {
                features.Add("Aura");
                _logger?.LogDebug("Aura support detected via keyboard light: Level={Level}", keyboardLight);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to detect Aura support");
        }
    }
}