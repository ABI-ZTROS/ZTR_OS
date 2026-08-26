using ZTR.Intelligence;
using ZTR.Models;

namespace ZTR.Intelligence.Tests;

public class PerformanceDecisionEngineTests
{
    private readonly PerformanceDecisionEngine _engine;

    public PerformanceDecisionEngineTests()
    {
        _engine = new PerformanceDecisionEngine(
            minSpl: 15, maxSpl: 65,
            minFanSpeed: 0, maxFanSpeed: 100,
            minCpuClockOffset: -500, maxCpuClockOffset: 500,
            minGpuClockOffset: -300, maxGpuClockOffset: 300);
    }

    [Fact]
    public void Decide_WithValidInput_ReturnsEightActions()
    {
        var decision = CreateDecision(0.5);

        var actions = _engine.Decide(decision);

        Assert.Equal(8, actions.Count);
    }

    [Fact]
    public void Decide_AllActionsWithinValidRanges()
    {
        var decision = CreateDecision(0.8);

        var actions = _engine.Decide(decision);

        var splAction = actions.First(a => a.ActionType == HardwareActionType.SplAdjustment);
        Assert.InRange(splAction.Value, 15, 65);

        var fanAction = actions.First(a => a.ActionType == HardwareActionType.FanCurveOffset);
        Assert.InRange(fanAction.Value, 0, 100);

        var cpuClockAction = actions.First(a => a.ActionType == HardwareActionType.CpuClockOffset);
        Assert.InRange(cpuClockAction.Value, -500, 500);

        var gpuClockAction = actions.First(a => a.ActionType == HardwareActionType.GpuClockOffset);
        Assert.InRange(gpuClockAction.Value, -300, 300);

        var boostAction = actions.First(a => a.ActionType == HardwareActionType.BoostLevel);
        Assert.InRange(boostAction.Value, 0, 3);
    }

    [Fact]
    public void Decide_ExtremeOutputs_ClampedToValidRange()
    {
        var decision = new MlpDecision
        {
            Timestamp = DateTime.Now,
            InputFeatures = new double[16],
            OutputActions = new double[] { 2.0, -1.0, 5.0, -5.0, 1.5, -0.5, 3.0, 10.0 },
            ActionType = "Test",
            Confidence = 0.9
        };

        var actions = _engine.Decide(decision);

        Assert.Equal(8, actions.Count);
        Assert.All(actions, a => Assert.NotNull(a.Reasoning));
    }

    [Fact]
    public void Decide_OutputBelowMin_ClampedToMin()
    {
        var decision = CreateDecision(0.0);

        var actions = _engine.Decide(decision);

        var splAction = actions.First(a => a.ActionType == HardwareActionType.SplAdjustment);
        Assert.Equal(15, splAction.Value);
    }

    [Fact]
    public void Decide_OutputAboveMax_ClampedToMax()
    {
        var decision = CreateDecision(1.0);

        var actions = _engine.Decide(decision);

        var splAction = actions.First(a => a.ActionType == HardwareActionType.SplAdjustment);
        Assert.Equal(65, splAction.Value);
    }

    [Fact]
    public void Decide_OutputMidRange_MappedCorrectly()
    {
        var decision = CreateDecision(0.5);

        var actions = _engine.Decide(decision);

        var splAction = actions.First(a => a.ActionType == HardwareActionType.SplAdjustment);
        Assert.Equal(40, splAction.Value);
    }

    [Fact]
    public void Decide_GpuMode_MappedCorrectly()
    {
        var decision = new MlpDecision
        {
            Timestamp = DateTime.Now,
            OutputActions = new double[] { 0.5, 0.5, 0.5, 0.5, 0.1, 0.5, 0.5, 0.5 }
        };

        var actions = _engine.Decide(decision);
        var gpuModeAction = actions.First(a => a.ActionType == HardwareActionType.GpuMode);
        Assert.Equal((int)GpuMode.Eco, gpuModeAction.Value);

        decision.OutputActions[4] = 0.9;
        actions = _engine.Decide(decision);
        gpuModeAction = actions.First(a => a.ActionType == HardwareActionType.GpuMode);
        Assert.Equal((int)GpuMode.Max, gpuModeAction.Value);
    }

    [Fact]
    public void Decide_NullDecision_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _engine.Decide(null!));
    }

    [Fact]
    public void Decide_InsufficientOutputActions_ThrowsArgumentException()
    {
        var decision = new MlpDecision
        {
            OutputActions = new double[3]
        };

        Assert.Throws<ArgumentException>(() => _engine.Decide(decision));
    }

    [Fact]
    public void Decide_AllActionsHaveReasoning()
    {
        var decision = CreateDecision(0.5);

        var actions = _engine.Decide(decision);

        foreach (var action in actions)
        {
            Assert.False(string.IsNullOrEmpty(action.Reasoning));
        }
    }

    [Fact]
    public void MapToRange_ValidInputs_ReturnsCorrectMappings()
    {
        Assert.Equal(0, PerformanceDecisionEngine.MapToRange(0.0, 0, 100));
        Assert.Equal(50, PerformanceDecisionEngine.MapToRange(0.5, 0, 100));
        Assert.Equal(100, PerformanceDecisionEngine.MapToRange(1.0, 0, 100));
        Assert.Equal(25, PerformanceDecisionEngine.MapToRange(0.25, 0, 100));
    }

    [Fact]
    public void MapToRange_OutOfRange_Clamped()
    {
        Assert.Equal(0, PerformanceDecisionEngine.MapToRange(-0.5, 0, 100));
        Assert.Equal(100, PerformanceDecisionEngine.MapToRange(1.5, 0, 100));
    }

    [Fact]
    public void Constructor_WithCustomParams_SetsProperties()
    {
        var engine = new PerformanceDecisionEngine(
            minSpl: 20, maxSpl: 80,
            minFanSpeed: 10, maxFanSpeed: 90,
            minCpuClockOffset: -300, maxCpuClockOffset: 300,
            minGpuClockOffset: -200, maxGpuClockOffset: 200);

        Assert.Equal(20, engine.MinSpl);
        Assert.Equal(80, engine.MaxSpl);
    }

    [Fact]
    public void Decide_AffinityMapping_RangesCorrectly()
    {
        var low = new MlpDecision
        {
            OutputActions = new double[] { 0.5, 0.5, 0.5, 0.5, 0.5, 0.1, 0.1, 0.5 }
        };
        var lowActions = _engine.Decide(low);
        var cpuAffinity = lowActions.First(a => a.ActionType == HardwareActionType.CpuAffinity);
        Assert.Equal(0, cpuAffinity.Value);

        var high = new MlpDecision
        {
            OutputActions = new double[] { 0.5, 0.5, 0.5, 0.5, 0.5, 0.9, 0.9, 0.5 }
        };
        var highActions = _engine.Decide(high);
        cpuAffinity = highActions.First(a => a.ActionType == HardwareActionType.CpuAffinity);
        Assert.Equal(3, cpuAffinity.Value);
    }

    private static MlpDecision CreateDecision(double value)
    {
        return new MlpDecision
        {
            Timestamp = DateTime.Now,
            InputFeatures = new double[16],
            OutputActions = new double[] { value, value, value, value, value, value, value, value },
            ActionType = "TestDecision",
            Confidence = 0.85,
            Reasoning = "Test reasoning"
        };
    }
}