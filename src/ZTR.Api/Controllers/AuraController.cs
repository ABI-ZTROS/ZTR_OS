using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuraController : ControllerBase
{
    private readonly AuraLighting _auraLighting;
    private readonly ILogger<AuraController> _logger;

    public AuraController(AuraLighting auraLighting, ILogger<AuraController> logger)
    {
        _auraLighting = auraLighting;
        _logger = logger;
    }

    [HttpGet("modes")]
    [ProducesResponseType<ApiResponse<IEnumerable<string>>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IEnumerable<string>>> ListModes()
    {
        var modes = Enum.GetNames(typeof(AuraMode));
        return Ok(new ApiResponse<IEnumerable<string>>(true, modes));
    }

    [HttpPost("apply")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> Apply([FromBody] SetAuraModeRequest request)
    {
        var result = _auraLighting.SetMode(request.Mode, request.Zone, request.R, request.G, request.B);
        if (!result)
        {
            return BadRequest(new ApiResponse(false, $"Failed to apply Aura mode {request.Mode}"));
        }

        return Ok(new ApiResponse(true));
    }

    [HttpGet("devices")]
    [ProducesResponseType<ApiResponse<List<AuraDeviceInfo>>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<AuraDeviceInfo>>> GetDevices()
    {
        var devices = new List<AuraDeviceInfo>
        {
            new() { Id = "keyboard", Name = "Keyboard", Zone = "keyboard", Type = "keyboard", CurrentEffect = _auraLighting.GetCurrentMode(AuraZone.Keyboard).ToString() },
            new() { Id = "body", Name = "Body", Zone = "body", Type = "body", CurrentEffect = _auraLighting.GetCurrentMode(AuraZone.Body).ToString() },
            new() { Id = "touchpad", Name = "Touchpad", Zone = "touchpad", Type = "touchpad", CurrentEffect = _auraLighting.GetCurrentMode(AuraZone.Touchpad).ToString() }
        };

        return Ok(new ApiResponse<List<AuraDeviceInfo>>(true, devices));
    }

    [HttpPost("devices/{deviceId}/effect")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> SetEffect(string deviceId, [FromBody] SetDeviceEffectRequest request)
    {
        if (!Enum.TryParse<AuraMode>(request.Effect, ignoreCase: true, out var mode))
        {
            return BadRequest(new ApiResponse(false, $"Unknown effect: {request.Effect}"));
        }

        var zone = deviceId switch
        {
            "keyboard" => AuraZone.Keyboard,
            "body" => AuraZone.Body,
            "touchpad" => AuraZone.Touchpad,
            _ => AuraZone.Keyboard
        };

        int r = request.Params?.Color?.R ?? 0;
        int g = request.Params?.Color?.G ?? 255;
        int b = request.Params?.Color?.B ?? 170;

        var result = _auraLighting.SetMode(mode, zone, (byte)r, (byte)g, (byte)b);
        if (!result)
        {
            return BadRequest(new ApiResponse(false, $"Failed to set effect {request.Effect} on {deviceId}"));
        }

        return Ok(new ApiResponse(true));
    }

    [HttpPost("devices/{deviceId}/color")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> SetColor(string deviceId, [FromBody] SetDeviceColorRequest request)
    {
        var hex = request.Color?.TrimStart('#');
        if (string.IsNullOrEmpty(hex) || hex.Length != 6)
        {
            return BadRequest(new ApiResponse(false, "Invalid color format. Use #RRGGBB"));
        }

        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);

        var zone = deviceId switch
        {
            "keyboard" => AuraZone.Keyboard,
            "body" => AuraZone.Body,
            "touchpad" => AuraZone.Touchpad,
            _ => AuraZone.Keyboard
        };

        var result = _auraLighting.SetMode(AuraMode.Static, zone, r, g, b);
        if (!result)
        {
            return BadRequest(new ApiResponse(false, $"Failed to set color on {deviceId}"));
        }

        return Ok(new ApiResponse(true));
    }

    [HttpPost("devices/{deviceId}/brightness")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> SetBrightness(string deviceId, [FromBody] SetBrightnessRequest request)
    {
        _auraLighting.SetBrightness(request.Brightness);
        return Ok(new ApiResponse(true));
    }

    [HttpGet("presets")]
    [ProducesResponseType<ApiResponse<List<AuraPresetInfo>>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<AuraPresetInfo>>> GetPresets()
    {
        var presets = new List<AuraPresetInfo>
        {
            new() { Id = "default-breathe", Name = "Default Breathe", Effect = "breathe", Zone = "keyboard" },
            new() { Id = "default-rainbow", Name = "Default Rainbow", Effect = "rainbow", Zone = "body" }
        };

        return Ok(new ApiResponse<List<AuraPresetInfo>>(true, presets));
    }

    [HttpPost("presets")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> SavePreset([FromBody] SavePresetRequest request)
    {
        _logger.LogInformation("Saving Aura preset: {Name}", request.Name);
        return Ok(new ApiResponse(true));
    }
}

public class AuraDeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? CurrentEffect { get; set; }
    public string? CurrentColor { get; set; }
}

public class SetDeviceEffectRequest
{
    public string Effect { get; set; } = string.Empty;
    public EffectParams? Params { get; set; }
}

public class EffectParams
{
    public int Brightness { get; set; } = 80;
    public int Speed { get; set; } = 50;
    public int Intensity { get; set; } = 70;
    public RgbColor? Color { get; set; }
}

public class RgbColor
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
}

public class SetDeviceColorRequest
{
    public string Color { get; set; } = string.Empty;
}

public class SetBrightnessRequest
{
    public int Brightness { get; set; }
}

public class SavePresetRequest
{
    public string Name { get; set; } = string.Empty;
}

public class AuraPresetInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Effect { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
}