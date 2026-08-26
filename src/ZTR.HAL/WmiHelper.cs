using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Provides WMI-based hardware information queries.
/// Uses <see cref="IWmiQueryService"/> to retrieve system, CPU, GPU, and BIOS details.
/// </summary>
public class WmiHelper
{
    private readonly IWmiQueryService _queryService;
    private readonly ILogger<WmiHelper>? _logger;

    /// <summary>
    /// Creates a new instance of the <see cref="WmiHelper"/> class.
    /// </summary>
    public WmiHelper()
        : this(new WmiQueryService(), null)
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="WmiHelper"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public WmiHelper(ILogger<WmiHelper>? logger)
        : this(new WmiQueryService(), logger)
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="WmiHelper"/> class with a specified query service.
    /// </summary>
    /// <param name="queryService">The WMI query service to use.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public WmiHelper(IWmiQueryService queryService, ILogger<WmiHelper>? logger = null)
    {
        _queryService = queryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the hardware model name from WMI (Win32_ComputerSystem).
    /// </summary>
    /// <returns>The model name, or an empty string if not available.</returns>
    public virtual string GetHardwareModel()
    {
        try
        {
            var results = _queryService.ExecuteQuery("SELECT Manufacturer, Model FROM Win32_ComputerSystem");
            foreach (var obj in results)
            {
                string? model = obj.ContainsKey("Model") ? obj["Model"]?.ToString() : null;
                string? manufacturer = obj.ContainsKey("Manufacturer") ? obj["Manufacturer"]?.ToString() : null;

                _logger?.LogDebug("WMI Hardware query: Manufacturer={Manufacturer}, Model={Model}", manufacturer, model);

                if (!string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(manufacturer))
                    return $"{manufacturer} {model}";

                return model ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to query Win32_ComputerSystem for hardware model");
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the BIOS version from WMI (Win32_BIOS).
    /// </summary>
    /// <returns>The BIOS version string, or an empty string if not available.</returns>
    public virtual string GetBiosVersion()
    {
        try
        {
            var results = _queryService.ExecuteQuery("SELECT SMBIOSBIOSVersion FROM Win32_BIOS");
            foreach (var obj in results)
            {
                string? version = obj.ContainsKey("SMBIOSBIOSVersion") ? obj["SMBIOSBIOSVersion"]?.ToString() : null;
                _logger?.LogDebug("WMI BIOS query: SMBIOSBIOSVersion={Version}", version);

                if (!string.IsNullOrWhiteSpace(version))
                    return version;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to query Win32_BIOS for BIOS version");
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets CPU information from WMI (Win32_Processor).
    /// </summary>
    /// <returns>The CPU name, or an empty string if not available.</returns>
    public virtual string GetCpuInfo()
    {
        try
        {
            var results = _queryService.ExecuteQuery("SELECT Name FROM Win32_Processor");
            foreach (var obj in results)
            {
                string? name = obj.ContainsKey("Name") ? obj["Name"]?.ToString() : null;
                _logger?.LogDebug("WMI CPU query: Name={Name}", name);

                if (!string.IsNullOrWhiteSpace(name))
                    return name.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to query Win32_Processor for CPU info");
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets GPU information from WMI (Win32_VideoController).
    /// </summary>
    /// <returns>The GPU name, or an empty string if not available.</returns>
    public virtual string GetGpuInfo()
    {
        try
        {
            var results = _queryService.ExecuteQuery("SELECT Name FROM Win32_VideoController");
            foreach (var obj in results)
            {
                string? name = obj.ContainsKey("Name") ? obj["Name"]?.ToString() : null;
                _logger?.LogDebug("WMI GPU query: Name={Name}", name);

                if (!string.IsNullOrWhiteSpace(name))
                    return name.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to query Win32_VideoController for GPU info");
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the manufacturer name from WMI (Win32_ComputerSystem).
    /// </summary>
    /// <returns>The manufacturer name, or an empty string if not available.</returns>
    public virtual string GetManufacturer()
    {
        try
        {
            var results = _queryService.ExecuteQuery("SELECT Manufacturer FROM Win32_ComputerSystem");
            foreach (var obj in results)
            {
                string? manufacturer = obj.ContainsKey("Manufacturer") ? obj["Manufacturer"]?.ToString() : null;
                _logger?.LogDebug("WMI Manufacturer query: Manufacturer={Manufacturer}", manufacturer);

                if (!string.IsNullOrWhiteSpace(manufacturer))
                    return manufacturer.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to query Win32_ComputerSystem for manufacturer");
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the device type from WMI based on the chassis type (Win32_SystemEnclosure).
    /// </summary>
    /// <returns>The device type, or <see cref="DeviceType.Unknown"/> if not determinable.</returns>
    public virtual DeviceType GetDeviceType()
    {
        try
        {
            var results = _queryService.ExecuteQuery("SELECT ChassisTypes FROM Win32_SystemEnclosure");
            foreach (var obj in results)
            {
                if (obj.ContainsKey("ChassisTypes") && obj["ChassisTypes"] is string[] chassisTypes)
                {
                    if (chassisTypes.Length > 0 && int.TryParse(chassisTypes[0], out int type))
                    {
                        _logger?.LogDebug("WMI ChassisType query: Type={Type}", type);
                        return type switch
                        {
                            3 or 4 or 5 or 6 or 7 => DeviceType.Desktop,
                            8 or 9 or 10 or 11 or 14 => DeviceType.Laptop,
                            12 or 18 or 21 => DeviceType.Tablet,
                            _ => DeviceType.Unknown
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to query Win32_SystemEnclosure for device type");
        }

        return DeviceType.Unknown;
    }
}