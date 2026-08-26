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
    public ActionResult<ApiResponse> AddRule([FromBody] AutomationRuleRequest request)
    {
        var rule = new AutomationRule
        {
            Trigger = Enum.Parse<PowerTrigger>(request.Trigger, true),
            PerformanceMode = request.PerformanceMode.HasValue ? Enum.Parse<AsusMode>(request.PerformanceMode.Value.ToString(), true) : null,
            GpuMode = request.GpuMode.HasValue ? Enum.Parse<AsusGPU>(request.GpuMode.Value.ToString(), true) : null,
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
    public ActionResult<ApiResponse> ApplyNow([FromBody] ApplyAutomationRequest request)
    {
        var result = _automationService.ApplyRulesForTrigger(Enum.Parse<PowerTrigger>(request.Trigger, true));
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