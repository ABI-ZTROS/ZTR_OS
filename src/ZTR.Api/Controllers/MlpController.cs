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
    private bool _isTraining;

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
    [ProducesResponseType<ApiResponse<MlpConfigResponse>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<MlpConfigResponse>> GetConfig()
    {
        var response = new MlpConfigResponse(
            LearningRate: _config.LearningRate,
            HiddenLayers: new[] { _config.HiddenLayerSize, Math.Max(_config.HiddenLayerSize / 2, 16) },
            InputSize: _config.InputSize,
            OutputSize: _config.OutputSize,
            IsTraining: _isTraining,
            Epochs: 0,
            CurrentEpoch: 0,
            Loss: 0);

        return Ok(new ApiResponse<MlpConfigResponse>(true, response));
    }

    [HttpPut("config")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> UpdateConfig([FromBody] MlpConfigUpdateRequest request)
    {
        var config = request.Config;
        _config.Enabled = config.IsTraining;
        _config.InputSize = config.InputSize;
        _config.HiddenLayerSize = config.HiddenLayers.FirstOrDefault(64);
        _config.OutputSize = config.OutputSize;
        _config.LearningRate = config.LearningRate;

        return Ok(new ApiResponse(true));
    }

    [HttpPost("train")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> StartTraining([FromBody] MlpConfigUpdateRequest request)
    {
        try
        {
            _isTraining = true;
            _config.Enabled = true;
            _logger.LogInformation("MLP training started");
            return Ok(new ApiResponse(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MLP training");
            return Ok(new ApiResponse(false, ex.Message));
        }
    }

    [HttpPost("stop")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> StopTraining()
    {
        _isTraining = false;
        _config.Enabled = false;
        _logger.LogInformation("MLP training stopped");
        return Ok(new ApiResponse(true));
    }

    [HttpPost("reset")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> ResetModel()
    {
        try
        {
            _isTraining = false;
            _network.SetWeights(
                new double[0, 0], System.Array.Empty<double>(),
                new double[0, 0], System.Array.Empty<double>(),
                new double[0, 0], System.Array.Empty<double>());
            _logger.LogInformation("MLP model reset");
            return Ok(new ApiResponse(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset MLP model");
            return Ok(new ApiResponse(false, ex.Message));
        }
    }

    [HttpGet("decisions")]
    [ProducesResponseType<ApiResponse<List<MlpDecisionResponse>>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<MlpDecisionResponse>>> GetDecisions(int count = 50)
    {
        var decisions = _decisionLogger.GetRecentDecisions(count);
        var response = decisions.Select(d => new MlpDecisionResponse(
            Id: d.Timestamp.ToString("o"),
            Timestamp: d.Timestamp,
            Input: d.InputFeatures,
            Output: d.OutputActions,
            Confidence: d.Confidence,
            Action: d.ActionType
        )).ToList();

        return Ok(new ApiResponse<List<MlpDecisionResponse>>(true, response));
    }

    [HttpGet("status")]
    [ProducesResponseType<ApiResponse<MlpStatusResponse>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<MlpStatusResponse>> GetStatus()
    {
        return Ok(new ApiResponse<MlpStatusResponse>(true, new MlpStatusResponse(
            Status: _isTraining ? "training" : "idle",
            Loss: 0,
            Epoch: 0
        )));
    }
}

public class MlpConfigResponse
{
    public double LearningRate { get; set; }
    public List<int> HiddenLayers { get; set; } = new();
    public int InputSize { get; set; }
    public int OutputSize { get; set; }
    public bool IsTraining { get; set; }
    public int Epochs { get; set; }
    public int CurrentEpoch { get; set; }
    public double Loss { get; set; }
}

public class MlpConfigUpdateRequest
{
    public MlpConfigResponse Config { get; set; } = new();
}

public class MlpDecisionResponse
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double[] Input { get; set; } = Array.Empty<double>();
    public double[] Output { get; set; } = Array.Empty<double>();
    public double Confidence { get; set; }
    public string Action { get; set; } = string.Empty;
}

public class MlpStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public double Loss { get; set; }
    public int Epoch { get; set; }
}
