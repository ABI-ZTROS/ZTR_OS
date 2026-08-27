using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
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
    private static UserSettings? _cachedSettings;
    private static readonly object _settingsLock = new();

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

        lock (_settingsLock)
        {
            _cachedSettings ??= new UserSettings();
        }

        var settings = new
        {
            Device = deviceInfo,
            PerformanceMode = mode,
            CpuFanCurve = cpuCurve,
            GpuFanCurve = gpuCurve,
            UserSettings = _cachedSettings
        };

        return Ok(new ApiResponse<object>(true, settings));
    }

    [HttpGet("user")]
    [ProducesResponseType<ApiResponse<UserSettings>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<UserSettings>> GetUserSettings()
    {
        lock (_settingsLock)
        {
            _cachedSettings ??= new UserSettings();
            return Ok(new ApiResponse<UserSettings>(true, _cachedSettings));
        }
    }

    [HttpPut("user")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> UpdateUserSettings([FromBody] UserSettingsRequest request)
    {
        lock (_settingsLock)
        {
            _cachedSettings ??= new UserSettings();

            if (request.Settings.AutoPerformance.HasValue)
                _cachedSettings.AutoPerformance = request.Settings.AutoPerformance.Value;
            if (request.Settings.AutoMlp.HasValue)
                _cachedSettings.AutoMlp = request.Settings.AutoMlp.Value;
            if (request.Settings.AutoAura.HasValue)
                _cachedSettings.AutoAura = request.Settings.AutoAura.Value;
            if (request.Settings.PollingInterval.HasValue)
                _cachedSettings.PollingInterval = request.Settings.PollingInterval.Value;
            if (!string.IsNullOrEmpty(request.Settings.Theme))
                _cachedSettings.Theme = request.Settings.Theme;
            if (request.Settings.NotificationsEnabled.HasValue)
                _cachedSettings.NotificationsEnabled = request.Settings.NotificationsEnabled.Value;
            if (request.Settings.AutoStart.HasValue)
                _cachedSettings.AutoStart = request.Settings.AutoStart.Value;
            if (request.Settings.MinimizeToTray.HasValue)
                _cachedSettings.MinimizeToTray = request.Settings.MinimizeToTray.Value;
            if (request.Settings.PredictionWindow.HasValue)
                _cachedSettings.PredictionWindow = request.Settings.PredictionWindow.Value;
            if (request.Settings.AutoModeSwitch.HasValue)
                _cachedSettings.AutoModeSwitch = request.Settings.AutoModeSwitch.Value;
            if (request.Settings.Hotkeys != null && request.Settings.Hotkeys.Count > 0)
                _cachedSettings.Hotkeys = request.Settings.Hotkeys;
        }

        _logger.LogInformation("User settings updated");
        return Ok(new ApiResponse(true));
    }

    [HttpPut]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> UpdateSettings([FromBody] PerformanceConfig config)
    {
        if (config == null)
        {
            return BadRequest(new ApiResponse(false, "Config cannot be null"));
        }

        if (config.Mode != _modeControl.CurrentMode)
        {
            var modeResult = _modeControl.SetMode(config.Mode);
            if (!modeResult)
            {
                return BadRequest(new ApiResponse(false, "Failed to set performance mode"));
            }
        }

        if (config.CpuFanCurve.Length > 0)
        {
            var cpuCurve = FanCurveCalculator.BytesToCurve(config.CpuFanCurve);
            var cpuResult = _modeControl.SetCpuFanCurve(cpuCurve);
            if (!cpuResult)
            {
                return BadRequest(new ApiResponse(false, "Failed to set CPU fan curve"));
            }
        }

        if (config.GpuFanCurve.Length > 0)
        {
            var gpuCurve = FanCurveCalculator.BytesToCurve(config.GpuFanCurve);
            var gpuResult = _modeControl.SetGpuFanCurve(gpuCurve);
            if (!gpuResult)
            {
                return BadRequest(new ApiResponse(false, "Failed to set GPU fan curve"));
            }
        }

        return Ok(new ApiResponse(true));
    }
}

public class UserSettingsRequest
{
    public UserSettingsUpdate Settings { get; set; } = new();
}

public class UserSettingsUpdate
{
    public bool? AutoPerformance { get; set; }
    public bool? AutoMlp { get; set; }
    public bool? AutoAura { get; set; }
    public int? PollingInterval { get; set; }
    public string? Theme { get; set; }
    public bool? NotificationsEnabled { get; set; }
    public bool? AutoStart { get; set; }
    public bool? MinimizeToTray { get; set; }
    public int? PredictionWindow { get; set; }
    public bool? AutoModeSwitch { get; set; }
    public List<HotkeySetting>? Hotkeys { get; set; }
}
