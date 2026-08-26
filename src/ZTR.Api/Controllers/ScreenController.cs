using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScreenController : ControllerBase
{
    private readonly ScreenControl _screenControl;
    private readonly KeyboardControl _keyboardControl;
    private readonly ILogger<ScreenController> _logger;

    public ScreenController(ScreenControl screenControl, KeyboardControl keyboardControl, ILogger<ScreenController> logger)
    {
        _screenControl = screenControl;
        _keyboardControl = keyboardControl;
        _logger = logger;
    }

    [HttpGet("refresh-rate")]
    public ActionResult<ApiResponse<object>> GetRefreshRate()
    {
        var rate = _screenControl.GetCurrentRefreshRate();
        var supported = _screenControl.GetSupportedRefreshRates();
        return Ok(new ApiResponse<object>(true, new { current = rate, supported }));
    }

    [HttpPost("refresh-rate")]
    public ActionResult<ApiResponse> SetRefreshRate([FromBody] SetRefreshRateRequest request)
    {
        var result = _screenControl.SetRefreshRate(request.Rate);
        return Ok(new ApiResponse(result));
    }

    [HttpGet("overdrive")]
    public ActionResult<ApiResponse<object>> GetOverdrive()
    {
        var enabled = _screenControl.GetOverdrive();
        return Ok(new ApiResponse<object>(true, new { enabled }));
    }

    [HttpPost("overdrive")]
    public ActionResult<ApiResponse> SetOverdrive([FromBody] SetOverdriveRequest request)
    {
        var result = _screenControl.SetOverdrive(request.Enabled);
        return Ok(new ApiResponse(result));
    }

    [HttpGet("mini-led")]
    public ActionResult<ApiResponse<object>> GetMiniLed()
    {
        var mode = _screenControl.GetMiniLed();
        return Ok(new ApiResponse<object>(true, new { mode = mode.ToString() }));
    }

    [HttpPost("mini-led")]
    public ActionResult<ApiResponse> SetMiniLed([FromBody] SetMiniLedRequest request)
    {
        if (!Enum.TryParse<MiniLedMode>(request.Mode, true, out var mode))
            return BadRequest(new ApiResponse(false, "Invalid MiniLED mode"));
        var result = _screenControl.SetMiniLed(mode);
        return Ok(new ApiResponse(result));
    }

    [HttpGet("hdr")]
    public ActionResult<ApiResponse<object>> GetHdr()
    {
        var enabled = _screenControl.GetHDR();
        return Ok(new ApiResponse<object>(true, new { enabled }));
    }

    [HttpPost("hdr")]
    public ActionResult<ApiResponse> SetHdr([FromBody] SetBoolRequest request)
    {
        var result = _screenControl.SetHDR(request.Enabled);
        return Ok(new ApiResponse(result));
    }

    [HttpGet("optimal-brightness")]
    public ActionResult<ApiResponse<object>> GetOptimalBrightness()
    {
        var enabled = _screenControl.GetOptimalBrightness();
        return Ok(new ApiResponse<object>(true, new { enabled }));
    }

    [HttpPost("optimal-brightness")]
    public ActionResult<ApiResponse> SetOptimalBrightness([FromBody] SetBoolRequest request)
    {
        var result = _screenControl.SetOptimalBrightness(request.Enabled);
        return Ok(new ApiResponse(result));
    }

    [HttpGet("keyboard-brightness")]
    public ActionResult<ApiResponse<object>> GetKeyboardBrightness()
    {
        var level = _keyboardControl.GetBrightness();
        return Ok(new ApiResponse<object>(true, new { level }));
    }

    [HttpPost("keyboard-brightness")]
    public ActionResult<ApiResponse> SetKeyboardBrightness([FromBody] SetBrightnessRequest request)
    {
        var result = _keyboardControl.SetBrightness(request.Level);
        return Ok(new ApiResponse(result));
    }
}

public class SetRefreshRateRequest { public int Rate { get; set; } }
public class SetOverdriveRequest { public bool Enabled { get; set; } }
public class SetMiniLedRequest { public string Mode { get; set; } = string.Empty; }
public class SetBoolRequest { public bool Enabled { get; set; } }
public class SetBrightnessRequest { public int Level { get; set; } }