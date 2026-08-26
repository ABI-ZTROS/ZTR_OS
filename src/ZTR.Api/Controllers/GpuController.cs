using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GpuController : ControllerBase
{
    private readonly GpuTuningService _tuningService;
    private readonly GPUModeControl _modeControl;
    private readonly IGpuControl _gpuControl;
    private readonly ILogger<GpuController> _logger;

    public GpuController(GpuTuningService tuningService, GPUModeControl modeControl, IGpuControl gpuControl, ILogger<GpuController> logger)
    {
        _tuningService = tuningService;
        _modeControl = modeControl;
        _gpuControl = gpuControl;
        _logger = logger;
    }

    [HttpGet("state")]
    public ActionResult<ApiResponse<object>> GetState()
    {
        var state = _tuningService.GetState();
        var live = _tuningService.GetLiveState();
        return Ok(new ApiResponse<object>(true, new { tuning = state, live }));
    }

    [HttpGet("clocks")]
    public ActionResult<ApiResponse<object>> GetClocks()
    {
        var state = _tuningService.GetState();
        return Ok(new ApiResponse<object>(true, new { state.CoreClockOffset, state.MemoryClockOffset }));
    }

    [HttpPost("clocks")]
    public ActionResult<ApiResponse> SetClocks([FromBody] SetClocksRequest request)
    {
        var result = _tuningService.SetBothClocks(request.CoreOffset, request.MemoryOffset);
        return Ok(new ApiResponse(result));
    }

    [HttpPost("power")]
    public ActionResult<ApiResponse> SetPower([FromBody] SetGpuPowerRequest request)
    {
        var result = _tuningService.SetPowerLimit(request.Watts);
        return Ok(new ApiResponse(result));
    }

    [HttpPost("temp-limit")]
    public ActionResult<ApiResponse> SetTempLimit([FromBody] SetTempLimitRequest request)
    {
        var result = _tuningService.SetTemperatureLimit(request.Temperature);
        return Ok(new ApiResponse(result));
    }

    [HttpPost("dynamic-boost")]
    public ActionResult<ApiResponse> SetDynamicBoost([FromBody] SetDynamicBoostRequest request)
    {
        var result = _tuningService.SetDynamicBoost(request.Level);
        return Ok(new ApiResponse(result));
    }

    [HttpPost("voltage")]
    public ActionResult<ApiResponse> SetVoltage([FromBody] SetVoltageRequest request)
    {
        var result = _tuningService.SetVoltageOffset(request.Offset);
        return Ok(new ApiResponse(result));
    }

    [HttpPost("reset")]
    public ActionResult<ApiResponse> ResetGpu()
    {
        var result = _tuningService.ResetAll();
        return Ok(new ApiResponse(result));
    }

    [HttpGet("mode")]
    public ActionResult<ApiResponse<object>> GetGpuMode()
    {
        var mode = _modeControl.GetGpuMode();
        var modeStr = mode.ToString().ToLowerInvariant();
        return Ok(new ApiResponse<object>(true, new { mode = modeStr }));
    }

    [HttpPost("mode")]
    public ActionResult<ApiResponse> SetGpuMode([FromBody] SetGpuModeRequest request)
    {
        if (!Enum.TryParse<AsusGPU>(request.Mode, true, out var gpuMode))
            return BadRequest(new ApiResponse(false, $"Invalid GPU mode: {request.Mode}"));
        var result = _modeControl.SetGpuMode(gpuMode);
        return Ok(new ApiResponse(result));
    }

    [HttpPost("optimized")]
    public ActionResult<ApiResponse> SetOptimizedMode()
    {
        // Optimized = Eco on battery, Standard on AC
        var isOnAc = SystemBatteryInfo.IsOnAcPower();
        var mode = isOnAc ? AsusGPU.Standard : AsusGPU.Eco;
        var result = _modeControl.SetGpuMode(mode);
        return Ok(new ApiResponse(result, $"Applied Optimized mode: {mode}"));
    }
}

public class SetClocksRequest { public int CoreOffset { get; set; } public int MemoryOffset { get; set; } }
public class SetGpuPowerRequest { public int Watts { get; set; } }
public class SetTempLimitRequest { public int Temperature { get; set; } }
public class SetDynamicBoostRequest { public int Level { get; set; } }
public class SetVoltageRequest { public int Offset { get; set; } }
public class SetGpuModeRequest { public string Mode { get; set; } = string.Empty; }