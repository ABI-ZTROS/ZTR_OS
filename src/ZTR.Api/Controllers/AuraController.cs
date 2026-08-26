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
    private static readonly List<AuraPresetInfo> _presets = new()
    {
        new() { Id = "default-breathe", Name = "Default Breathe", Effect = "breathe", Zone = "keyboard" },
        new() { Id = "default-rainbow", Name = "Default Rainbow", Effect = "rainbow", Zone = "body" }
    };
    private static readonly object _presetsLock = new();

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
        var effectName = request.Effect?.ToLowerInvariant();
        if (!TryParseAuraEffect(effectName, out var mode))
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

        byte r = 0, g = 255, b = 170;
        if (!string.IsNullOrEmpty(request.Color))
        {
            var (cr, cg, cb) = ParseHexColor(request.Color);
            r = cr; g = cg; b = cb;
        }
        else if (request.Params?.Color != null)
        {
            r = (byte)Math.Clamp(request.Params.Color.R, 0, 255);
            g = (byte)Math.Clamp(request.Params.Color.G, 0, 255);
            b = (byte)Math.Clamp(request.Params.Color.B, 0, 255);
        }

        int speed = request.Params?.Speed ?? 50;
        int intensity = request.Params?.Intensity ?? 70;

        _auraLighting.SetBrightness(request.Params?.Brightness ?? 80);

        var result = _auraLighting.SetMode(mode, zone, r, g, b, speed, 0, (byte)intensity);
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
        var (r, g, b) = ParseHexColor(request.Color);

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

        var zone = deviceId switch
        {
            "keyboard" => AuraZone.Keyboard,
            "body" => AuraZone.Body,
            "touchpad" => AuraZone.Touchpad,
            _ => AuraZone.Keyboard
        };

        var currentColor = _auraLighting.CurrentColor;
        _auraLighting.SetMode(_auraLighting.CurrentMode, zone,
            (byte)(currentColor.R * request.Brightness / 100),
            (byte)(currentColor.G * request.Brightness / 100),
            (byte)(currentColor.B * request.Brightness / 100));

        return Ok(new ApiResponse(true));
    }

    [HttpPost("devices/{deviceId}/speed")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> SetSpeed(string deviceId, [FromBody] SetSpeedRequest request)
    {
        var zone = deviceId switch
        {
            "keyboard" => AuraZone.Keyboard,
            "body" => AuraZone.Body,
            "touchpad" => AuraZone.Touchpad,
            _ => AuraZone.Keyboard
        };

        _auraLighting.SetMode(_auraLighting.CurrentMode, zone,
            _auraLighting.CurrentColor.R,
            _auraLighting.CurrentColor.G,
            _auraLighting.CurrentColor.B,
            request.Speed);

        return Ok(new ApiResponse(true));
    }

    [HttpPost("devices/{deviceId}/intensity")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> SetIntensity(string deviceId, [FromBody] SetIntensityRequest request)
    {
        var zone = deviceId switch
        {
            "keyboard" => AuraZone.Keyboard,
            "body" => AuraZone.Body,
            "touchpad" => AuraZone.Touchpad,
            _ => AuraZone.Keyboard
        };

        _auraLighting.SetMode(_auraLighting.CurrentMode, zone,
            _auraLighting.CurrentColor.R,
            _auraLighting.CurrentColor.G,
            _auraLighting.CurrentColor.B,
            0, 0, (byte)request.Intensity);

        return Ok(new ApiResponse(true));
    }

    [HttpPost("devices/{deviceId}/enable")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> SetEnable(string deviceId, [FromBody] SetEnableRequest request)
    {
        if (!request.Enabled)
        {
            _auraLighting.TurnOffAll();
        }
        else
        {
            var zone = deviceId switch
            {
                "keyboard" => AuraZone.Keyboard,
                "body" => AuraZone.Body,
                "touchpad" => AuraZone.Touchpad,
                _ => AuraZone.Keyboard
            };
            var color = _auraLighting.CurrentColor;
            _auraLighting.SetMode(AuraMode.Breathe, zone, color.R, color.G, color.B);
        }

        return Ok(new ApiResponse(true));
    }

    [HttpGet("presets")]
    [ProducesResponseType<ApiResponse<List<AuraPresetInfo>>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<AuraPresetInfo>>> GetPresets()
    {
        lock (_presetsLock)
        {
            return Ok(new ApiResponse<List<AuraPresetInfo>>(true, new List<AuraPresetInfo>(_presets)));
        }
    }

    [HttpPost("presets")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> SavePreset([FromBody] SavePresetRequest request)
    {
        lock (_presetsLock)
        {
            var preset = new AuraPresetInfo
            {
                Id = $"preset-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Name = request.Name,
                Effect = _auraLighting.CurrentMode.ToString(),
                Zone = _auraLighting.CurrentZone.ToString().ToLowerInvariant()
            };
            _presets.Add(preset);
            _logger.LogInformation("Aura preset saved: {Name} (Effect: {Effect}, Zone: {Zone})", preset.Name, preset.Effect, preset.Zone);
        }

        return Ok(new ApiResponse(true));
    }

    private static bool TryParseAuraEffect(string? effectName, out AuraMode mode)
    {
        mode = effectName switch
        {
            "static" => AuraMode.Static,
            "breathe" => AuraMode.Breathe,
            "rainbow" => AuraMode.Rainbow,
            "audio" => AuraMode.Audio,
            "heatmap" => AuraMode.Heatmap,
            "wave" => AuraMode.ColorCycle,
            "ripple" => AuraMode.Ripple,
            "starry" => AuraMode.Star,
            _ => AuraMode.Static
        };
        return effectName is "static" or "breathe" or "rainbow" or "audio" or "heatmap" or "wave" or "ripple" or "starry";
    }

    private static (byte R, byte G, byte B) ParseHexColor(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return (0, 255, 170);

        hex = hex.TrimStart('#');
        if (hex.Length != 6)
            return (0, 255, 170);

        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
        return (r, g, b);
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
    public string? Color { get; set; }
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

public class SetSpeedRequest
{
    public int Speed { get; set; }
}

public class SetIntensityRequest
{
    public int Intensity { get; set; }
}

public class SetEnableRequest
{
    public bool Enabled { get; set; }
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