using Microsoft.AspNetCore.Mvc;
using ZTR.Intelligence;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MlpController : ControllerBase
{
    private readonly MlpNetwork _network;
    private readonly MlpConfig _config;
    private readonly DecisionLogger _decisionLogger;
    private readonly PredictiveScheduler _scheduler;
    private readonly ILogger<MlpController> _logger;

    public MlpController(
        MlpNetwork network,
        MlpConfig config,
        DecisionLogger decisionLogger,
        PredictiveScheduler scheduler,
        ILogger<MlpController> logger)
    {
        _network = network;
        _config = config;
        _decisionLogger = decisionLogger;
        _scheduler = scheduler;
        _logger = logger;
    }

    [HttpGet("config")]
    [ProducesResponseType<ApiResponse<MlpConfig>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<MlpConfig>> GetConfig()
    {
        return Ok(new ApiResponse<MlpConfig>(true, _config));
    }

    [HttpPut("config")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> UpdateConfig([FromBody] MlpConfigUpdateRequest request)
    {
        var config = request.Config;
        _config.Enabled = config.Enabled;
        _config.InputSize = config.InputSize;
        _config.HiddenLayerSize = config.HiddenLayerSize;
        _config.OutputSize = config.OutputSize;
        _config.LearningRate = config.LearningRate;
        _config.LearningIntervalSeconds = config.LearningIntervalSeconds;
        _config.PredictionWindowMs = config.PredictionWindowMs;
        _config.AutoModeSwitch = config.AutoModeSwitch;
        _config.AutoAffinity = config.AutoAffinity;

        return Ok(new ApiResponse(true));
    }

    [HttpGet("decisions")]
    [ProducesResponseType<ApiResponse<MlpDecision[]>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<MlpDecision[]>> GetDecisions(int count = 50)
    {
        var decisions = _decisionLogger.GetRecentDecisions(count);
        return Ok(new ApiResponse<MlpDecision[]>(true, decisions));
    }

    [HttpGet("status")]
    [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<bool>> GetStatus()
    {
        return Ok(new ApiResponse<bool>(true, _config.Enabled));
    }
}