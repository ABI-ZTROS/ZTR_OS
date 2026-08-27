using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutomationController : ControllerBase
{
    private readonly AutomationService _automationService;
    private readonly ILogger<AutomationController> _logger;

    public AutomationController(AutomationService automationService, ILogger<AutomationController> logger)
    {
        _automationService = automationService;
        _logger = logger;
    }

    [HttpGet("status")]
    public ActionResult<ApiResponse<object>> GetStatus()
    {
        var config = _automationService.GetConfig();
        return Ok(new ApiResponse<object>(true, new
        {
            isRunning = _automationService.IsRunning,
            isEnabled = config.IsEnabled,
            rules = config.Rules.Select(r => new
            {
                trigger = r.Trigger.ToString(),
                performanceMode = r.PerformanceMode?.ToString(),
                gpuMode = r.GpuMode?.ToString(),
                refreshRate = r.RefreshRate,
                keyboardTimeout = r.KeyboardTimeoutSeconds,
                chargeLimit = r.ChargeLimit,
                optimizeGpu = r.OptimizeGpu,
                name = r.Name
            })
        }));
    }

    [HttpPost("start")]
    public ActionResult<ApiResponse> Start()
    {
        _automationService.Start();
        return Ok(new ApiResponse(true, "Automation service started"));
    }

    [HttpPost("stop")]
    public ActionResult<ApiResponse> Stop()
    {
        _automationService.Stop();
        return Ok(new ApiResponse(true, "Automation service stopped"));
    }

    [HttpGet("rules")]
    public ActionResult<ApiResponse<object>> GetRules()
    {
        var config = _automationService.GetConfig();
        return Ok(new ApiResponse<object>(true, config.Rules));
    }

    [HttpPost("rules")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> AddRule([FromBody] AutomationRuleRequest request)
    {
        if (!Enum.TryParse<PowerTrigger>(request.Trigger, true, out var trigger))
        {
            return BadRequest(new ApiResponse(false, $"Invalid trigger: {request.Trigger}"));
        }

        AsusMode? performanceMode = null;
        if (request.PerformanceMode.HasValue)
        {
            if (!Enum.TryParse<AsusMode>(request.PerformanceMode.Value.ToString(), true, out var pm))
            {
                return BadRequest(new ApiResponse(false, $"Invalid performance mode: {request.PerformanceMode}"));
            }
            performanceMode = pm;
        }

        AsusGPU? gpuMode = null;
        if (request.GpuMode.HasValue)
        {
            if (!Enum.TryParse<AsusGPU>(request.GpuMode.Value.ToString(), true, out var gm))
            {
                return BadRequest(new ApiResponse(false, $"Invalid GPU mode: {request.GpuMode}"));
            }
            gpuMode = gm;
        }

        var rule = new AutomationRule
        {
            Trigger = trigger,
            PerformanceMode = performanceMode,
            GpuMode = gpuMode,
            RefreshRate = request.RefreshRate,
            KeyboardTimeoutSeconds = request.KeyboardTimeoutSeconds,
            ChargeLimit = request.ChargeLimit,
            OptimizeGpu = request.OptimizeGpu,
            Name = request.Name ?? $"Rule-{request.Trigger}"
        };

        _automationService.AddRule(rule);
        return Ok(new ApiResponse(true));
    }

    [HttpDelete("rules/{name}")]
    public ActionResult<ApiResponse> RemoveRule(string name)
    {
        _automationService.RemoveRule(name);
        return Ok(new ApiResponse(true));
    }

    [HttpPut("config")]
    public ActionResult<ApiResponse> UpdateConfig([FromBody] AutomationConfigRequest request)
    {
        var config = _automationService.GetConfig();
        config.IsEnabled = request.IsEnabled;
        _automationService.UpdateConfig(config);
        return Ok(new ApiResponse(true));
    }

    [HttpPost("apply")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> ApplyNow([FromBody] ApplyAutomationRequest request)
    {
        if (!Enum.TryParse<PowerTrigger>(request.Trigger, true, out var trigger))
        {
            return BadRequest(new ApiResponse(false, $"Invalid trigger: {request.Trigger}"));
        }

        var result = _automationService.ApplyRulesForTrigger(trigger);
        return Ok(new ApiResponse(result.success, result.message));
    }
}

public class AutomationRuleRequest
{
    public string Trigger { get; set; } = "AC";
    public int? PerformanceMode { get; set; }
    public int? GpuMode { get; set; }
    public int? RefreshRate { get; set; }
    public int? KeyboardTimeoutSeconds { get; set; }
    public int? ChargeLimit { get; set; }
    public bool OptimizeGpu { get; set; }
    public string? Name { get; set; }
}

public class AutomationConfigRequest
{
    public bool IsEnabled { get; set; }
}

public class ApplyAutomationRequest
{
    public string Trigger { get; set; } = "AC";
}