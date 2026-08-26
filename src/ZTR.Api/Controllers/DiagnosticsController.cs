using Microsoft.AspNetCore.Mvc;
using System.Security.Principal;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly AsusAcpi _acpi;
    private readonly AsusHid _hid;
    private readonly ModeControl _modeControl;
    private readonly DeviceProbe _deviceProbe;
    private readonly SensorPipeline _sensorPipeline;
    private readonly ISystemSensorFallback _systemFallback;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        AsusAcpi acpi,
        AsusHid hid,
        ModeControl modeControl,
        DeviceProbe deviceProbe,
        SensorPipeline sensorPipeline,
        ISystemSensorFallback systemFallback,
        ILogger<DiagnosticsController> logger)
    {
        _acpi = acpi;
        _hid = hid;
        _modeControl = modeControl;
        _deviceProbe = deviceProbe;
        _sensorPipeline = sensorPipeline;
        _systemFallback = systemFallback;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<DiagnosticsReport>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<DiagnosticsReport>> Get()
    {
        var deviceInfo = _deviceProbe.Probe();
        var report = new DiagnosticsReport
        {
            Timestamp = DateTime.UtcNow,
            MachineName = Environment.MachineName,
            OSVersion = Environment.OSVersion.VersionString,
            IsAdmin = IsRunningAsAdministrator(),
            Services = new ServiceStatus
            {
                AcpiAvailable = _acpi.IsAvailable,
                HidInitialized = _hid.IsInitialized,
                HidDeviceCount = _hid.DeviceCount,
                ModeControlActive = true,
                SystemFallbackAvailable = _systemFallback.IsAvailable,
                SystemFallbackInitProgress = _systemFallback.GetInitializationProgress(),
                SupportedDevices = deviceInfo.SupportedFeatures.ToList(),
                AtkDevicePath = GetAtkDevicePath()
            }
        };

        try
        {
            var state = _sensorPipeline.CollectOnce();
            report.LastHardwareState = state;
            report.Services.LastCollectionSucceeded = true;
        }
        catch (Exception ex)
        {
            report.Services.LastCollectionSucceeded = false;
            report.Services.LastCollectionError = ex.Message;
            _logger.LogWarning(ex, "Diagnostics: sensor pipeline collect failed");
        }

        return Ok(new ApiResponse<DiagnosticsReport>(true, report));
    }

    [HttpGet("acpi/status")]
    [ProducesResponseType<ApiResponse<AcpiStatus>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<AcpiStatus>> GetAcpiStatus()
    {
        var deviceInfo = _deviceProbe.Probe();
        var status = new AcpiStatus
        {
            IsAvailable = _acpi.IsAvailable,
            AtkDevicePath = GetAtkDevicePath(),
            SupportedDevices = deviceInfo.SupportedFeatures.ToList()
        };

        return Ok(new ApiResponse<AcpiStatus>(true, status));
    }

    [HttpGet("system/info")]
    [ProducesResponseType<ApiResponse<SystemInfo>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<SystemInfo>> GetSystemInfo()
    {
        var info = new SystemInfo
        {
            MachineName = Environment.MachineName,
            OSVersion = Environment.OSVersion.VersionString,
            Is64Bit = Environment.Is64BitOperatingSystem,
            ProcessorCount = Environment.ProcessorCount,
            IsAdmin = IsRunningAsAdministrator(),
            RuntimeVersion = Environment.Version.ToString(),
            StartupTime = DateTime.UtcNow.AddMilliseconds(-Environment.TickCount64)
        };

        return Ok(new ApiResponse<SystemInfo>(true, info));
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private string GetAtkDevicePath()
    {
        try
        {
            var field = typeof(AsusAcpi).GetField("_device", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(_acpi) is IAtkDevice device)
            {
                return device.OpenedPath;
            }
        }
        catch { }
        return "unknown";
    }
}

public class DiagnosticsReport
{
    public DateTime Timestamp { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public ServiceStatus Services { get; set; } = new();
    public HardwareState? LastHardwareState { get; set; }
}

public class ServiceStatus
{
    public bool AcpiAvailable { get; set; }
    public bool HidInitialized { get; set; }
    public int HidDeviceCount { get; set; }
    public bool ModeControlActive { get; set; }
    public bool SystemFallbackAvailable { get; set; }
    public int SystemFallbackInitProgress { get; set; }
    public List<string> SupportedDevices { get; set; } = new();
    public string AtkDevicePath { get; set; } = string.Empty;
    public bool LastCollectionSucceeded { get; set; }
    public string? LastCollectionError { get; set; }
}

public class AcpiStatus
{
    public bool IsAvailable { get; set; }
    public string AtkDevicePath { get; set; } = string.Empty;
    public List<string> SupportedDevices { get; set; } = new();
}

public class SystemInfo
{
    public string MachineName { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;
    public bool Is64Bit { get; set; }
    public int ProcessorCount { get; set; }
    public bool IsAdmin { get; set; }
    public string RuntimeVersion { get; set; } = string.Empty;
    public DateTime StartupTime { get; set; }
}
