using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ModeControl _modeControl;
    private readonly DeviceProbe _deviceProbe;
    private readonly SensorPipeline _sensorPipeline;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ModeControl modeControl,
        DeviceProbe deviceProbe,
        SensorPipeline sensorPipeline,
        ILogger<SettingsController> logger)
    {
        _modeControl = modeControl;
        _deviceProbe = deviceProbe;
        _sensorPipeline = sensorPipeline;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetSettings()
    {
        var mode = _modeControl.GetCurrentMode();
        var deviceInfo = _deviceProbe.Probe();
        var cpuCurve = _modeControl.GetFanCurve(AsusFan.CPU);
        var gpuCurve = _modeControl.GetFanCurve(AsusFan.GPU);

        var settings = new
        {
            Device = deviceInfo,
            PerformanceMode = mode,
            CpuFanCurve = cpuCurve,
            GpuFanCurve = gpuCurve
        };

        return Ok(new ApiResponse<object>(true, settings));
    }

    [HttpPut]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> UpdateSettings([FromBody] PerformanceConfig config)
    {
        if (config.Mode != _modeControl.CurrentMode)
        {
            _modeControl.SetMode(config.Mode);
        }

        if (config.CpuFanCurve.Length > 0)
        {
            var cpuCurve = FanCurveCalculator.BytesToCurve(config.CpuFanCurve);
            _modeControl.SetCpuFanCurve(cpuCurve);
        }

        if (config.GpuFanCurve.Length > 0)
        {
            var gpuCurve = FanCurveCalculator.BytesToCurve(config.GpuFanCurve);
            _modeControl.SetGpuFanCurve(gpuCurve);
        }

        return Ok(new ApiResponse(true));
    }
}