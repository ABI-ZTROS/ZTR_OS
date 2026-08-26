using ZTR.Models;

namespace ZTR.Intelligence;

/// <summary>
/// A predictive scheduler that analyzes hardware state trends and pre-emptively
/// adjusts hardware settings before bottlenecks occur. Uses the MLP network
/// to predict near-term optimal configurations.
/// </summary>
public class PredictiveScheduler
{
    private readonly MlpNetwork _network;
    private readonly SensorFeatureExtractor _featureExtractor;
    private readonly PerformanceDecisionEngine _decisionEngine;
    private readonly int _predictionWindowMs;
    private readonly List<HardwareState> _recentStates;
    private readonly object _lock = new();

    private MlpDecision? _lastDecision;
    private IReadOnlyList<HardwareAction> _predictedActions = Array.Empty<HardwareAction>();

    /// <summary>
    /// Gets the prediction window duration in milliseconds.
    /// </summary>
    public int PredictionWindowMs => _predictionWindowMs;

    /// <summary>
    /// Gets the last decision made by the scheduler.
    /// </summary>
    public MlpDecision? LastDecision
    {
        get { lock (_lock) { return _lastDecision; } }
    }

    /// <summary>
    /// Gets the current predicted actions.
    /// </summary>
    public IReadOnlyList<HardwareAction> PredictedActions
    {
        get { lock (_lock) { return _predictedActions; } }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PredictiveScheduler"/> class.
    /// </summary>
    /// <param name="network">The trained MLP network for predictions.</param>
    /// <param name="featureExtractor">Feature extraction from sensor data.</param>
    /// <param name="decisionEngine">Maps MLP output to hardware actions.</param>
    /// <param name="predictionWindowMs">Pre-emptive adjustment window in milliseconds.</param>
    public PredictiveScheduler(
        MlpNetwork network,
        SensorFeatureExtractor featureExtractor,
        PerformanceDecisionEngine decisionEngine,
        int predictionWindowMs = 500)
    {
        ArgumentNullException.ThrowIfNull(network);
        ArgumentNullException.ThrowIfNull(featureExtractor);
        ArgumentNullException.ThrowIfNull(decisionEngine);

        _network = network;
        _featureExtractor = featureExtractor;
        _decisionEngine = decisionEngine;
        _predictionWindowMs = Math.Clamp(predictionWindowMs, 100, 5000);
        _recentStates = new List<HardwareState>();
    }

    /// <summary>
    /// Main scheduler loop update. Processes accumulated state history and generates
    /// predictive hardware actions based on current trends.
    /// </summary>
    /// <returns>The predicted hardware actions for the near-term future.</returns>
    public IReadOnlyList<HardwareAction> Update()
    {
        lock (_lock)
        {
            if (_recentStates.Count < 2)
                return _predictedActions;

            var current = _recentStates[_recentStates.Count - 1];
            var history = _recentStates.Take(Math.Min(_featureExtractor.WindowSeconds * 2, _recentStates.Count - 1)).ToArray();

            double[] features = _featureExtractor.ExtractFeatures(current, history);
            double[] actions = _network.Predict(features);

            double confidence = ComputeConfidence(actions);

            var decision = new MlpDecision
            {
                Timestamp = DateTime.Now,
                InputFeatures = features,
                OutputActions = actions,
                ActionType = "PredictiveSchedule",
                Confidence = confidence,
                Reasoning = GenerateReasoning(actions, current)
            };

            _lastDecision = decision;
            _predictedActions = _decisionEngine.Decide(decision);

            return _predictedActions;
        }
    }

    /// <summary>
    /// Triggered when a new sensor reading is available. Stores the state and
    /// returns predicted actions based on the updated history.
    /// </summary>
    /// <param name="state">The latest hardware state from the sensor pipeline.</param>
    /// <returns>Predicted hardware actions reflecting the new sensor data.</returns>
    public IReadOnlyList<HardwareAction> OnSensorUpdate(HardwareState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_lock)
        {
            _recentStates.Add(state);

            if (_recentStates.Count > 120)
                _recentStates.RemoveAt(0);
        }

        return Update();
    }

    /// <summary>
    /// Gets the current predicted near-term hardware actions without performing
    /// a new update cycle.
    /// </summary>
    /// <returns>The cached predicted actions from the last update.</returns>
    public IReadOnlyList<HardwareAction> GetPredictedActions()
    {
        lock (_lock)
        {
            return _predictedActions;
        }
    }

    /// <summary>
    /// Computes a confidence score based on output variance (higher confidence = more peaked output).
    /// </summary>
    private static double ComputeConfidence(double[] actions)
    {
        if (actions.Length == 0)
            return 0.0;

        double mean = actions.Average();
        double variance = actions.Select(a => (a - mean) * (a - mean)).Sum() / actions.Length;
        double stdDev = Math.Sqrt(variance);

        return Math.Clamp(1.0 - stdDev * 2.0, 0.0, 1.0);
    }

    /// <summary>
    /// Generates a human-readable reasoning string explaining the decision.
    /// </summary>
    private static string GenerateReasoning(double[] actions, HardwareState state)
    {
        var reasons = new List<string>();

        if (actions[0] > 0.75)
            reasons.Add("High SPL demand detected");
        else if (actions[0] < 0.25)
            reasons.Add("Low power consumption");

        if (state.Cpu.Temperature > 80)
            reasons.Add("CPU temperature elevated");
        if (state.Gpu.Temperature > 75)
            reasons.Add("GPU temperature elevated");

        if (state.Cpu.Usage > 80 || state.Gpu.Usage > 80)
            reasons.Add("High system load");

        if (reasons.Count == 0)
            reasons.Add("Normal operating conditions");

        return string.Join("; ", reasons);
    }

    /// <summary>
    /// Clears the internal state history.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _recentStates.Clear();
            _lastDecision = null;
            _predictedActions = Array.Empty<HardwareAction>();
        }
    }
}