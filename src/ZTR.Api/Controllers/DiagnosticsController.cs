using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        AsusAcpi acpi,
        AsusHid hid,
        ModeControl modeControl,
        DeviceProbe deviceProbe,
        SensorPipeline sensorPipeline,
        ILogger<DiagnosticsController> logger)
    {
        _acpi = acpi;
        _hid = hid;
        _modeControl = modeControl;
        _deviceProbe = deviceProbe;
        _sensorPipeline = sensorPipeline;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<DiagnosticsReport>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<DiagnosticsReport>> Get()
    {
        var report = new DiagnosticsReport
        {
            Timestamp = DateTime.UtcNow,
            Services = new ServiceStatus
            {
                AspiAvailable = _acpi.IsAvailable,
                AcpiAvailable = _acpi.IsAvailable,
                HidInitialized = _hid.IsInitialized,
                HidDeviceCount = _hid.DeviceCount,
                ModeControlActive = true
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
}

public class DiagnosticsReport
{
    public DateTime Timestamp { get; set; }
    public ServiceStatus Services { get; set; } = new();
    public HardwareState? LastHardwareState { get; set; }
}

public class ServiceStatus
{
    public bool AspiAvailable { get; set; }
    public bool AcpiAvailable { get; set; }
    public bool HidInitialized { get; set; }
    public int HidDeviceCount { get; set; }
    public bool ModeControlActive { get; set; }
    public bool LastCollectionSucceeded { get; set; }
    public string? LastCollectionError { get; set; }
}
