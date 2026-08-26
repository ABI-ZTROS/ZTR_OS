using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HardwareController : ControllerBase
{
    private readonly SensorPipeline _sensorPipeline;
    private readonly ILogger<HardwareController> _logger;

    public HardwareController(
        SensorPipeline sensorPipeline,
        ILogger<HardwareController> logger)
    {
        _sensorPipeline = sensorPipeline;
        _logger = logger;
    }

    [HttpGet("state")]
    [ProducesResponseType<ApiResponse<HardwareState>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<HardwareState>> GetState()
    {
        var state = _sensorPipeline.CollectOnce();
        return Ok(new ApiResponse<HardwareState>(true, state));
    }

    [HttpGet("cpu")]
    [ProducesResponseType<ApiResponse<CpuState>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<CpuState>> GetCpu()
    {
        var state = _sensorPipeline.CollectOnce();
        return Ok(new ApiResponse<CpuState>(true, state.Cpu));
    }

    [HttpGet("gpu")]
    [ProducesResponseType<ApiResponse<GpuState>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<GpuState>> GetGpu()
    {
        var state = _sensorPipeline.CollectOnce();
        return Ok(new ApiResponse<GpuState>(true, state.Gpu));
    }

    [HttpGet("battery")]
    [ProducesResponseType<ApiResponse<BatteryState>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<BatteryState>> GetBattery()
    {
        var state = _sensorPipeline.CollectOnce();
        return Ok(new ApiResponse<BatteryState>(true, state.Battery));
    }

    [HttpGet("fan")]
    [ProducesResponseType<ApiResponse<FanState>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<FanState>> GetFan()
    {
        var state = _sensorPipeline.CollectOnce();
        return Ok(new ApiResponse<FanState>(true, state.Fan));
    }
}