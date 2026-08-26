using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PerformanceController : ControllerBase
{
    private readonly ModeControl _modeControl;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(
        ModeControl modeControl,
        ILogger<PerformanceController> logger)
    {
        _modeControl = modeControl;
        _logger = logger;
    }

    [HttpGet("mode")]
    [ProducesResponseType<ApiResponse<AsusMode>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<AsusMode>> GetMode()
    {
        var mode = _modeControl.GetCurrentMode();
        return Ok(new ApiResponse<AsusMode>(true, mode));
    }

    [HttpPost("mode")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> SetMode([FromBody] SetPerformanceModeRequest request)
    {
        var result = _modeControl.SetMode(request.Mode);
        if (!result)
        {
            return BadRequest(new ApiResponse(false, $"Failed to set performance mode to {request.Mode}"));
        }

        return Ok(new ApiResponse(true));
    }

    [HttpGet("fan-curves")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetFanCurves()
    {
        var cpuCurve = _modeControl.GetFanCurve(AsusFan.CPU);
        var gpuCurve = _modeControl.GetFanCurve(AsusFan.GPU);
        var midCurve = _modeControl.GetFanCurve(AsusFan.Mid);

        var curves = new
        {
            Cpu = cpuCurve,
            Gpu = gpuCurve,
            Mid = midCurve
        };

        return Ok(new ApiResponse<object>(true, curves));
    }

    [HttpPost("fan-curves")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> SetFanCurves([FromBody] SetFanCurveRequest request)
    {
        var points = FanCurveCalculator.BytesToCurve(request.Curve);
        var result = request.Device switch
        {
            AsusFan.CPU => _modeControl.SetCpuFanCurve(points),
            AsusFan.GPU => _modeControl.SetGpuFanCurve(points),
            AsusFan.Mid => _modeControl.SetMidFanCurve(points),
            _ => false
        };

        if (!result)
        {
            return BadRequest(new ApiResponse(false, $"Failed to set fan curve for {request.Device}"));
        }

        return Ok(new ApiResponse(true));
    }

    [HttpPost("power-limits")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> SetPowerLimits([FromBody] SetPowerLimitRequest request)
    {
        var result = _modeControl.SetPowerLimits(request.SPL, request.SPPT, request.FPPT);
        if (!result)
        {
            return BadRequest(new ApiResponse(false, "Failed to set power limits"));
        }

        return Ok(new ApiResponse(true));
    }
}