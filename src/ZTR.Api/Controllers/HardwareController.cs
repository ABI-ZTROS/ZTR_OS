using Microsoft.AspNetCore.Mvc;
using ZTR.Api.DTOs;
using ZTR.Api.Mappers;
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
    [ProducesResponseType<ApiResponse<HardwareResponse>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<HardwareResponse>> GetState()
    {
        var state = _sensorPipeline.CollectOnce();
        var response = HardwareMapper.ToFrontend(state);
        return Ok(new ApiResponse<HardwareResponse>(true, response));
    }

    [HttpGet("cpu")]
    [ProducesResponseType<ApiResponse<CpuResponse>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<CpuResponse>> GetCpu()
    {
        var state = _sensorPipeline.CollectOnce();
        var mapped = HardwareMapper.ToFrontend(state);
        return Ok(new ApiResponse<CpuResponse>(true, mapped.Cpu));
    }

    [HttpGet("gpu")]
    [ProducesResponseType<ApiResponse<GpuResponse>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<GpuResponse>> GetGpu()
    {
        var state = _sensorPipeline.CollectOnce();
        var mapped = HardwareMapper.ToFrontend(state);
        return Ok(new ApiResponse<GpuResponse>(true, mapped.Gpu));
    }

    [HttpGet("battery")]
    [ProducesResponseType<ApiResponse<BatteryResponse>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<BatteryResponse>> GetBattery()
    {
        var state = _sensorPipeline.CollectOnce();
        var mapped = HardwareMapper.ToFrontend(state);
        return Ok(new ApiResponse<BatteryResponse>(true, mapped.Battery));
    }

    [HttpGet("fan")]
    [ProducesResponseType<ApiResponse<List<FanResponse>>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<FanResponse>>> GetFan()
    {
        var state = _sensorPipeline.CollectOnce();
        var mapped = HardwareMapper.ToFrontend(state);
        return Ok(new ApiResponse<List<FanResponse>>(true, mapped.Fans));
    }
}
