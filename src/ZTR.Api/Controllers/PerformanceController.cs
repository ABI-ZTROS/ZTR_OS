using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PerformanceController : ControllerBase
{
    private readonly ModeControl _modeControl;
    private readonly PowerLimitManager _powerManager;
    private readonly GPUModeControl _gpuModeControl;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(
        ModeControl modeControl,
        PowerLimitManager powerManager,
        GPUModeControl gpuModeControl,
        ILogger<PerformanceController> logger)
    {
        _modeControl = modeControl;
        _powerManager = powerManager;
        _gpuModeControl = gpuModeControl;
        _logger = logger;
    }

    [HttpGet("mode")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetMode()
    {
        var mode = _modeControl.GetCurrentMode();
        var modeStr = mode switch
        {
            AsusMode.PerformanceSilent => "silent",
            AsusMode.PerformanceBalanced => "balanced",
            AsusMode.PerformanceTurbo => "turbo",
            AsusMode.PerformanceFullSpeed => "fullspeed",
            AsusMode.PerformanceManual => "manual",
            _ => "balanced"
        };
        return Ok(new ApiResponse<object>(true, new { mode = modeStr }));
    }

    [HttpPost("mode")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> SetMode([FromBody] SetPerformanceModeRequest request)
    {
        if (!TryParseMode(request.Mode, out var mode))
        {
            return BadRequest(new ApiResponse(false, $"Unknown performance mode: {request.Mode}"));
        }

        var result = _modeControl.SetMode(mode);
        if (!result)
        {
            return BadRequest(new ApiResponse(false, $"Failed to set performance mode to {request.Mode}"));
        }

        return Ok(new ApiResponse(true));
    }

    [HttpGet("power-limits")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetPowerLimits()
    {
        var state = _powerManager.GetPowerState();
        return Ok(new ApiResponse<object>(true, new
        {
            cpu = state.SPL,
            gpu = 0,
            spl = state.SPL,
            sppt = state.SPPT,
            fppt = state.FPPT
        }));
    }

    [HttpPost("power-limits")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> SetPowerLimits([FromBody] SetPowerLimitRequest request)
    {
        int spl = request.SPL ?? _powerManager.GetPowerState().SPL;
        int sppt = request.SPPT ?? _powerManager.GetPowerState().SPPT;
        int fppt = request.FPPT ?? _powerManager.GetPowerState().FPPT;

        var result = _modeControl.SetPowerLimits(spl, sppt, fppt);
        if (!result)
        {
            return BadRequest(new ApiResponse(false, "Failed to set power limits"));
        }

        return Ok(new ApiResponse(true));
    }

    [HttpPost("cpu-power")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> SetCpuPower([FromBody] SetCpuPowerRequest request)
    {
        var result = _powerManager.SetSPL(request.Watts);
        return Ok(new ApiResponse(result));
    }

    [HttpPost("gpu-power")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> SetGpuPower([FromBody] SetGpuPowerRequest request)
    {
        try
        {
            var acpi = _modeControl.GetAcpi();
            int result = acpi.DeviceSet(AsusDevice.PPT_GPUC0, request.Watts, $"SetGpuPower({request.Watts}W)");
            return Ok(new ApiResponse(result == 1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set GPU power limit");
            return Ok(new ApiResponse(false, ex.Message));
        }
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
            cpu = cpuCurve.Select(p => p.Speed).ToArray(),
            gpu = gpuCurve.Select(p => p.Speed).ToArray(),
            mid = midCurve.Select(p => p.Speed).ToArray()
        };

        return Ok(new ApiResponse<object>(true, curves));
    }

    [HttpPost("fan-curves")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> SetFanCurves([FromBody] SetFanCurveRequest request)
    {
        var points = SpeedArrayToCurve(request.Curve);
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

    private static bool TryParseMode(string modeStr, out AsusMode mode)
    {
        mode = modeStr switch
        {
            "silent" => AsusMode.PerformanceSilent,
            "balanced" => AsusMode.PerformanceBalanced,
            "turbo" => AsusMode.PerformanceTurbo,
            "fullspeed" => AsusMode.PerformanceFullSpeed,
            "manual" => AsusMode.PerformanceManual,
            _ => AsusMode.PerformanceBalanced
        };
        return modeStr is "silent" or "balanced" or "turbo" or "fullspeed" or "manual";
    }

    private static FanCurvePoint[] SpeedArrayToCurve(int[] speeds)
    {
        if (speeds == null || speeds.Length == 0)
            return Array.Empty<FanCurvePoint>();

        var points = new FanCurvePoint[Math.Min(speeds.Length, FanCurveCalculator.CurvePointCount)];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new FanCurvePoint
            {
                Temperature = (byte)(30 + i * 10),
                Speed = (int)Math.Clamp(speeds[i], 0, 100)
            };
        }
        return points;
    }
}

public class SetCpuPowerRequest
{
    public int Watts { get; set; }
}

public class SetGpuPowerRequest
{
    public int Watts { get; set; }
}