using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
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
    private readonly MlpTrainingState _trainingState;
    private readonly SensorQueue _queue;
    private readonly SensorFeatureExtractor _featureExtractor;
    private readonly PerformanceDecisionEngine _decisionEngine;
    private readonly ILogger<MlpController> _logger;

    public MlpController(
        MlpNetwork network,
        MlpConfig config,
        DecisionLogger decisionLogger,
        PredictiveScheduler scheduler,
        MlpTrainingState trainingState,
        SensorQueue queue,
        SensorFeatureExtractor featureExtractor,
        PerformanceDecisionEngine decisionEngine,
        ILogger<MlpController> logger)
    {
        _network = network;
        _config = config;
        _decisionLogger = decisionLogger;
        _scheduler = scheduler;
        _trainingState = trainingState;
        _queue = queue;
        _featureExtractor = featureExtractor;
        _decisionEngine = decisionEngine;
        _logger = logger;
    }

    [HttpGet("config")]
    [ProducesResponseType<ApiResponse<MlpConfigResponse>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<MlpConfigResponse>> GetConfig()
    {
        var response = new MlpConfigResponse
        {
            LearningRate = _config.LearningRate,
            HiddenLayers = new List<int> { _config.HiddenLayerSize, Math.Max(_config.HiddenLayerSize / 2, 16) },
            InputSize = _config.InputSize,
            OutputSize = _config.OutputSize,
            IsTraining = _trainingState.IsTraining,
            Epochs = _trainingState.CurrentEpoch,
            CurrentEpoch = _trainingState.CurrentEpoch,
            Loss = _trainingState.Loss
        };

        return Ok(new ApiResponse<MlpConfigResponse>(true, response));
    }

    [HttpPut("config")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> UpdateConfig([FromBody] MlpConfigUpdateRequest request)
    {
        var config = request.Config;

        // V5 FIXED: Detect dimension changes and rebuild network if needed
        bool dimensionsChanged = _config.InputSize != config.InputSize
            || _config.OutputSize != config.OutputSize
            || _config.HiddenLayerSize != config.HiddenLayers.FirstOrDefault(64);

        _config.Enabled = config.IsTraining;
        _config.InputSize = config.InputSize;
        _config.HiddenLayerSize = config.HiddenLayers.FirstOrDefault(64);
        _config.OutputSize = config.OutputSize;
        _config.LearningRate = config.LearningRate;

        if (dimensionsChanged)
        {
            // V5 FIXED: Rebuild network with new dimensions instead of keeping a mismatched one
            _logger.LogInformation("MLP dimensions changed, rebuilding network: Input={Input}, Output={Output}, Hidden={Hidden}",
                _config.InputSize, _config.OutputSize, _config.HiddenLayerSize);
            var newNetwork = new MlpNetwork(_config);
            // Copy weights isn't possible across dimensions; just reset
            _network.SetWeights(
                newNetwork.GetWeights().w1, newNetwork.GetWeights().b1,
                newNetwork.GetWeights().w2, newNetwork.GetWeights().b2,
                newNetwork.GetWeights().w3, newNetwork.GetWeights().b3);
        }

        return Ok(new ApiResponse(true));
    }

    [HttpPost("train")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> StartTraining([FromBody] MlpConfigUpdateRequest request)
    {
        if (_trainingState.IsTraining)
        {
            return Ok(new ApiResponse(false, "Training already in progress"));
        }

        try
        {
            _trainingState.Start();
            _config.Enabled = true;
            _logger.LogInformation("MLP training started");

            // V5 FIXED: Actually run training on sensor data instead of just setting a flag
            var cts = new CancellationTokenSource();
            _ = TrainLoopAsync(cts.Token);

            return Ok(new ApiResponse(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MLP training");
            _trainingState.Stop();
            return Ok(new ApiResponse(false, ex.Message));
        }
    }

    private async Task TrainLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            var learner = new OnlineLearner(_network, _config.LearningRate);
            int epoch = 0;
            const int maxEpochs = 50;

            while (!cancellationToken.IsCancellationRequested && epoch < maxEpochs)
            {
                // Get recent hardware states for training data
                var history = _queue.GetHistory(100);
                if (history.Count < 2)
                {
                    _logger.LogWarning("Not enough sensor data for training ({Count} samples). Waiting for more data...", history.Count);
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                // Build training samples from history
                var samples = new List<MlpTrainingSample>();
                for (int i = 1; i < history.Count; i++)
                {
                    var prev = history[i - 1];
                    var curr = history[i];

                    var features = _featureExtractor.ExtractFeatures(curr, new[] { prev });
                    var target = _decisionEngine.ComputeTarget(curr);

                    if (features.Length == _config.InputSize && target.Length == _config.OutputSize)
                    {
                        samples.Add(new MlpTrainingSample
                        {
                            Features = features,
                            Target = target,
                            Timestamp = curr.Timestamp
                        });
                    }
                }

                if (samples.Count == 0)
                {
                    await Task.Delay(500, cancellationToken);
                    continue;
                }

                // Train on batch
                var batch = samples.ToArray();
                double loss = learner.TrainBatch(batch);
                epoch++;

                _trainingState.UpdateProgress(epoch, loss);
                _trainingState.IncrementSamples(batch.Length);
                _logger.LogDebug("MLP training epoch {Epoch}: loss={Loss:F4}, samples={Samples}", epoch, loss, batch.Length);

                // Progress via SignalR
                try
                {
                    if (_scheduler is { } s)
                    {
                        // Trigger prediction update
                    }
                }
                catch { }

                await Task.Delay(100, cancellationToken);
            }

            _trainingState.Complete();
            _logger.LogInformation("MLP training completed: {Epoch} epochs, final loss={Loss:F4}", epoch, _trainingState.Loss);
        }
        catch (OperationCanceledException)
        {
            _trainingState.Complete();
            _logger.LogInformation("MLP training stopped by user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MLP training failed");
            _trainingState.Complete();
        }
    }

    [HttpPost("stop")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> StopTraining()
    {
        _trainingState.Stop();
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
            // V5 FIXED: Create a fresh network with proper dimensions instead of zero-dimension arrays
            // that would cause IndexOutOfRange on Predict
            _trainingState.Stop();
            var freshNetwork = new MlpNetwork(_config);
            var (w1, b1, w2, b2, w3, b3) = freshNetwork.GetWeights();
            _network.SetWeights(w1, b1, w2, b2, w3, b3);
            _trainingState.UpdateProgress(0, 0);
            _logger.LogInformation("MLP model reset with fresh weights (Input={Input}, Output={Output})",
                _config.InputSize, _config.OutputSize);
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
        var response = decisions.Select(d => new MlpDecisionResponse
        {
            Id = d.Timestamp.ToString("o"),
            Timestamp = d.Timestamp,
            Input = d.InputFeatures,
            Output = d.OutputActions,
            Confidence = d.Confidence,
            Action = d.ActionType
        }).ToList();

        return Ok(new ApiResponse<List<MlpDecisionResponse>>(true, response));
    }

    [HttpGet("status")]
    [ProducesResponseType<ApiResponse<MlpStatusResponse>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<MlpStatusResponse>> GetStatus()
    {
        // V5 FIXED: Use singleton training state instead of per-request instance field
        return Ok(new ApiResponse<MlpStatusResponse>(true, new MlpStatusResponse
        {
            Status = _trainingState.IsTraining ? "training" : "idle",
            Loss = _trainingState.Loss,
            Epoch = _trainingState.CurrentEpoch
        }));
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
