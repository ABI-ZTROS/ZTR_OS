using ZTR.Intelligence;
using ZTR.Models;

namespace ZTR.Intelligence.Tests;

public class PredictiveSchedulerTests
{
    private readonly MlpNetwork _network;
    private readonly SensorFeatureExtractor _featureExtractor;
    private readonly PerformanceDecisionEngine _decisionEngine;
    private readonly PredictiveScheduler _scheduler;

    public PredictiveSchedulerTests()
    {
        _network = new MlpNetwork(inputSize: 20, hiddenSize: 32, outputSize: 8, seed: 42);
        _featureExtractor = new SensorFeatureExtractor(windowSeconds: 5, featureCount: 20);
        _decisionEngine = new PerformanceDecisionEngine();
        _scheduler = new PredictiveScheduler(_network, _featureExtractor, _decisionEngine, predictionWindowMs: 500);
    }

    [Fact]
    public void Constructor_ValidParams_CreatesInstance()
    {
        Assert.Equal(500, _scheduler.PredictionWindowMs);
        Assert.Null(_scheduler.LastDecision);
    }

    [Fact]
    public void Constructor_NullNetwork_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PredictiveScheduler(null!, _featureExtractor, _decisionEngine));
    }

    [Fact]
    public void Constructor_NullFeatureExtractor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PredictiveScheduler(_network, null!, _decisionEngine));
    }

    [Fact]
    public void Constructor_NullDecisionEngine_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PredictiveScheduler(_network, _featureExtractor, null!));
    }

    [Fact]
    public void OnSensorUpdate_SingleState_ReturnsEmptyActions()
    {
        var state = CreateTestState();

        var actions = _scheduler.OnSensorUpdate(state);

        Assert.Empty(actions);
    }

    [Fact]
    public void OnSensorUpdate_MultipleStates_ProducesActions()
    {
        var states = CreateStateSequence(5);

        IReadOnlyList<HardwareAction> actions = Array.Empty<HardwareAction>();
        foreach (var state in states)
        {
            actions = _scheduler.OnSensorUpdate(state);
        }

        Assert.NotEmpty(actions);
        Assert.Equal(8, actions.Count);
    }

    [Fact]
    public void OnSensorUpdate_RecordsLastDecision()
    {
        var states = CreateStateSequence(5);
        foreach (var state in states)
        {
            _scheduler.OnSensorUpdate(state);
        }

        Assert.NotNull(_scheduler.LastDecision);
        Assert.Equal(8, _scheduler.LastDecision!.OutputActions.Length);
    }

    [Fact]
    public void OnSensorUpdate_NullState_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _scheduler.OnSensorUpdate(null!));
    }

    [Fact]
    public void Update_WithInsufficientHistory_ReturnsEmpty()
    {
        var actions = _scheduler.Update();

        Assert.Empty(actions);
    }

    [Fact]
    public void Update_WithSufficientHistory_ReturnsActions()
    {
        var states = CreateStateSequence(10);
        foreach (var state in states)
        {
            _scheduler.OnSensorUpdate(state);
        }

        var actions = _scheduler.Update();

        Assert.NotEmpty(actions);
    }

    [Fact]
    public void GetPredictedActions_ReturnsCachedActions()
    {
        var states = CreateStateSequence(5);
        foreach (var state in states)
        {
            _scheduler.OnSensorUpdate(state);
        }

        var actions = _scheduler.GetPredictedActions();

        Assert.NotEmpty(actions);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var states = CreateStateSequence(5);
        foreach (var state in states)
        {
            _scheduler.OnSensorUpdate(state);
        }

        _scheduler.Reset();

        Assert.Null(_scheduler.LastDecision);
        Assert.Empty(_scheduler.GetPredictedActions());
    }

    [Fact]
    public void OnSensorUpdate_AllActionsHaveValidProperties()
    {
        var states = CreateStateSequence(5);
        foreach (var state in states)
        {
            _scheduler.OnSensorUpdate(state);
        }

        var actions = _scheduler.GetPredictedActions();

        foreach (var action in actions)
        {
            Assert.NotNull(action);
            Assert.False(string.IsNullOrEmpty(action.Reasoning));
        }
    }

    [Fact]
    public void Constructor_CustomWindow_ClampsToValidRange()
    {
        var scheduler = new PredictiveScheduler(_network, _featureExtractor, _decisionEngine, predictionWindowMs: -100);
        Assert.Equal(100, scheduler.PredictionWindowMs);

        scheduler = new PredictiveScheduler(_network, _featureExtractor, _decisionEngine, predictionWindowMs: 10000);
        Assert.Equal(5000, scheduler.PredictionWindowMs);
    }

    [Fact]
    public void OnSensorUpdate_ManyStates_DoesNotExceedBuffer()
    {
        for (int i = 0; i < 200; i++)
        {
            _scheduler.OnSensorUpdate(CreateTestState());
        }

        var actions = _scheduler.GetPredictedActions();
        Assert.NotEmpty(actions);
    }

    private static HardwareState CreateTestState()
    {
        return new HardwareState
        {
            Cpu = new CpuState { Temperature = 65, Usage = 45, Power = 120, ClockMHz = 3200 },
            Gpu = new GpuState { Temperature = 55, Usage = 60, Power = 180, CoreClockMHz = 1800 },
            Battery = new BatteryState { ChargePercent = 75, IsCharging = true },
            Fan = new FanState { CpuFanSpeed = 45, GpuFanSpeed = 50 },
            Timestamp = DateTime.Now
        };
    }

    private static HardwareState[] CreateStateSequence(int count)
    {
        var states = new HardwareState[count];
        var now = DateTime.Now;

        for (int i = 0; i < count; i++)
        {
            states[i] = new HardwareState
            {
                Cpu = new CpuState
                {
                    Temperature = 60 + i * 2,
                    Usage = 40 + i * 3,
                    Power = 100 + i * 10,
                    ClockMHz = 3000 + i * 100
                },
                Gpu = new GpuState
                {
                    Temperature = 50 + i * 2,
                    HotspotTemperature = 58 + i * 2,
                    Usage = 50 + i * 4,
                    Power = 150 + i * 12,
                    CoreClockMHz = 1700 + i * 50,
                    TotalVramMB = 8192,
                    UsedVramMB = 4096 + i * 200
                },
                Battery = new BatteryState { ChargePercent = 75, IsCharging = true },
                Fan = new FanState { CpuFanSpeed = 40 + i * 2, GpuFanSpeed = 45 + i * 2 },
                Timestamp = now.AddSeconds(-count + i + 1)
            };
        }

        return states;
    }
}