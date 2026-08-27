using ZTR.Models;

namespace ZTR.Intelligence;

/// <summary>
/// Maps the 8-dimensional MLP output vector to concrete hardware actions.
/// Validates that actions remain within safe hardware limits before outputting.
/// </summary>
public class PerformanceDecisionEngine
{
    private readonly int _minSpl;
    private readonly int _maxSpl;
    private readonly int _minFanSpeed;
    private readonly int _maxFanSpeed;
    private readonly int _minCpuClockOffset;
    private readonly int _maxCpuClockOffset;
    private readonly int _minGpuClockOffset;
    private readonly int _maxGpuClockOffset;

    /// <summary>
    /// Gets the minimum SPL value in watts.
    /// </summary>
    public int MinSpl => _minSpl;

    /// <summary>
    /// Gets the maximum SPL value in watts.
    /// </summary>
    public int MaxSpl => _maxSpl;

    /// <summary>
    /// Creates a new instance of the <see cref="PerformanceDecisionEngine"/> class.
    /// </summary>
    /// <param name="minSpl">Minimum SPL in watts.</param>
    /// <param name="maxSpl">Maximum SPL in watts.</param>
    /// <param name="minFanSpeed">Minimum fan speed percentage.</param>
    /// <param name="maxFanSpeed">Maximum fan speed percentage.</param>
    /// <param name="minCpuClockOffset">Minimum CPU clock offset in MHz.</param>
    /// <param name="maxCpuClockOffset">Maximum CPU clock offset in MHz.</param>
    /// <param name="minGpuClockOffset">Minimum GPU clock offset in MHz.</param>
    /// <param name="maxGpuClockOffset">Maximum GPU clock offset in MHz.</param>
    public PerformanceDecisionEngine(
        int minSpl = 15,
        int maxSpl = 65,
        int minFanSpeed = 0,
        int maxFanSpeed = 100,
        int minCpuClockOffset = -500,
        int maxCpuClockOffset = 500,
        int minGpuClockOffset = -300,
        int maxGpuClockOffset = 300)
    {
        _minSpl = minSpl;
        _maxSpl = maxSpl;
        _minFanSpeed = minFanSpeed;
        _maxFanSpeed = maxFanSpeed;
        _minCpuClockOffset = minCpuClockOffset;
        _maxCpuClockOffset = maxCpuClockOffset;
        _minGpuClockOffset = minGpuClockOffset;
        _maxGpuClockOffset = maxGpuClockOffset;
    }

    /// <summary>
    /// Converts an MLP decision into a list of validated hardware actions.
    /// </summary>
    /// <param name="decision">The MLP decision containing output actions.</param>
    /// <returns>A list of hardware action commands ready for execution.</returns>
    public IReadOnlyList<HardwareAction> Decide(MlpDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.OutputActions.Length < 8)
            throw new ArgumentException("Output actions must have at least 8 dimensions.");

        var actions = new List<HardwareAction>(8);

        double[] output = decision.OutputActions;

        int spl = MapToRange(output[0], _minSpl, _maxSpl);
        actions.Add(new HardwareAction
        {
            ActionType = HardwareActionType.SplAdjustment,
            Value = spl,
            Reasoning = $"MLP suggests SPL={spl}W based on confidence {decision.Confidence:F2}"
        });

        int fanOffset = MapToRange(output[1], _minFanSpeed, _maxFanSpeed);
        actions.Add(new HardwareAction
        {
            ActionType = HardwareActionType.FanCurveOffset,
            Value = fanOffset,
            Reasoning = $"MLP suggests fan curve offset to {fanOffset}%"
        });

        int gpuClockOffset = MapToRange(output[2], _minGpuClockOffset, _maxGpuClockOffset);
        actions.Add(new HardwareAction
        {
            ActionType = HardwareActionType.GpuClockOffset,
            Value = gpuClockOffset,
            Reasoning = $"MLP suggests GPU clock offset {gpuClockOffset}MHz"
        });

        int cpuClockOffset = MapToRange(output[3], _minCpuClockOffset, _maxCpuClockOffset);
        actions.Add(new HardwareAction
        {
            ActionType = HardwareActionType.CpuClockOffset,
            Value = cpuClockOffset,
            Reasoning = $"MLP suggests CPU clock offset {cpuClockOffset}MHz"
        });

        var gpuMode = MapToGpuMode(output[4]);
        actions.Add(new HardwareAction
        {
            ActionType = HardwareActionType.GpuMode,
            Value = (int)gpuMode,
            Reasoning = $"MLP suggests GPU mode: {gpuMode}"
        });

        var cpuAffinity = MapToAffinity(output[5]);
        actions.Add(new HardwareAction
        {
            ActionType = HardwareActionType.CpuAffinity,
            Value = cpuAffinity,
            Reasoning = $"MLP suggests CPU affinity: group {cpuAffinity}"
        });

        var gpuAffinity = MapToAffinity(output[6]);
        actions.Add(new HardwareAction
        {
            ActionType = HardwareActionType.GpuAffinity,
            Value = gpuAffinity,
            Reasoning = $"MLP suggests GPU affinity: group {gpuAffinity}"
        });

        int boostLevel = MapToRange(output[7], 0, 3);
        actions.Add(new HardwareAction
        {
            ActionType = HardwareActionType.BoostLevel,
            Value = boostLevel,
            Reasoning = $"MLP suggests boost level: {boostLevel}"
        });

        return ValidateActions(actions);
    }

    /// <summary>
    /// Maps a normalized [0,1] output value to an integer in the specified range.
    /// </summary>
    /// <param name="normalized">Normalized value in [0, 1].</param>
    /// <param name="min">Minimum output value.</param>
    /// <param name="max">Maximum output value.</param>
    /// <returns>The mapped integer value.</returns>
    public static int MapToRange(double normalized, int min, int max)
    {
        double clamped = Math.Clamp(normalized, 0.0, 1.0);
        return (int)Math.Round(min + clamped * (max - min));
    }

    /// <summary>
    /// Maps a normalized output to a GPU mode.
    /// </summary>
    private static GpuMode MapToGpuMode(double value)
    {
        return value switch
        {
            < 0.25 => GpuMode.Eco,
            < 0.5 => GpuMode.Performance,
            < 0.75 => GpuMode.Turbo,
            _ => GpuMode.Max
        };
    }

    /// <summary>
    /// Maps a normalized output to an affinity group index (0-3).
    /// </summary>
    private static int MapToAffinity(double value)
    {
        return value switch
        {
            < 0.25 => 0,
            < 0.5 => 1,
            < 0.75 => 2,
            _ => 3
        };
    }

    /// <summary>
    /// Validates all actions remain within configured limits and clamps any that don't.
    /// </summary>
    private IReadOnlyList<HardwareAction> ValidateActions(IReadOnlyList<HardwareAction> actions)
    {
        var validated = new List<HardwareAction>(actions.Count);

        foreach (var action in actions)
        {
            var clampedValue = action.ActionType switch
            {
                HardwareActionType.SplAdjustment => Math.Clamp(action.Value, _minSpl, _maxSpl),
                HardwareActionType.FanCurveOffset => Math.Clamp(action.Value, _minFanSpeed, _maxFanSpeed),
                HardwareActionType.CpuClockOffset => Math.Clamp(action.Value, _minCpuClockOffset, _maxCpuClockOffset),
                HardwareActionType.GpuClockOffset => Math.Clamp(action.Value, _minGpuClockOffset, _maxGpuClockOffset),
                HardwareActionType.BoostLevel => Math.Clamp(action.Value, 0, 3),
                _ => action.Value
            };

            validated.Add(new HardwareAction
            {
                ActionType = action.ActionType,
                Value = clampedValue,
                Reasoning = action.Reasoning
            });
        }

        return validated;
    }

    /// <summary>
    /// Computes the ideal target output vector for a given hardware state.
    /// This is used during training to provide the MLP with supervised learning targets.
    /// Maps the current hardware conditions to the normalized [0,1] output space.
    /// </summary>
    /// <param name="state">The current hardware state.</param>
    /// <returns>An 8-dimensional target output vector in [0,1] range.</returns>
    public double[] ComputeTarget(HardwareState state)
    {
        var target = new double[8];

        // Dimension 0: SPL (power limit) - higher when GPU is under load
        double splScore = (state.Gpu.Usage > 70 || state.Cpu.Usage > 70) ? 0.8 : 0.5;
        if (state.Battery.IsCharging) splScore = Math.Min(1.0, splScore + 0.1);
        target[0] = splScore;

        // Dimension 1: Fan speed offset - higher when temperatures are high
        double tempScore = Math.Max(state.Cpu.Temperature, state.Gpu.Temperature) / 100.0;
        target[1] = Math.Clamp(tempScore, 0.0, 1.0);

        // Dimension 2: GPU clock offset - negative when hot, positive when cool
        target[2] = state.Gpu.Temperature > 80 ? 0.2 : 0.7;

        // Dimension 3: CPU clock offset - similar logic
        target[3] = state.Cpu.Temperature > 75 ? 0.3 : 0.7;

        // Dimension 4: GPU mode - Eco on low battery, Turbo on AC + high load
        if (!state.Battery.IsCharging && state.Battery.ChargePercent < 30)
            target[4] = 0.15; // Eco
        else if (state.Gpu.Usage > 80)
            target[4] = 0.85; // Max
        else
            target[4] = 0.5; // Performance

        // Dimension 5: CPU affinity - group 2 (value 0.5) as default
        target[5] = 0.5;

        // Dimension 6: GPU affinity - group 2 (value 0.5) as default
        target[6] = 0.5;

        // Dimension 7: Boost level - higher when AC and high load
        target[7] = (state.Battery.IsCharging && (state.Cpu.Usage > 60 || state.Gpu.Usage > 60)) ? 0.75 : 0.5;

        return target;
    }
}

/// <summary>
/// Represents a single hardware action command.
/// </summary>
public class HardwareAction
{
    /// <summary>
    /// Gets or sets the type of hardware action.
    /// </summary>
    public HardwareActionType ActionType { get; set; }

    /// <summary>
    /// Gets or sets the numeric value of the action (e.g., wattage, speed percentage).
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the reasoning string explaining why this action was chosen.
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// Enumeration of hardware action types that the MLP can control.
/// </summary>
public enum HardwareActionType
{
    /// <summary>
    /// Short Power Limit adjustment in watts.
    /// </summary>
    SplAdjustment,

    /// <summary>
    /// Fan curve offset percentage.
    /// </summary>
    FanCurveOffset,

    /// <summary>
    /// GPU clock speed offset in MHz.
    /// </summary>
    GpuClockOffset,

    /// <summary>
    /// CPU clock speed offset in MHz.
    /// </summary>
    CpuClockOffset,

    /// <summary>
    /// GPU operating mode selection.
    /// </summary>
    GpuMode,

    /// <summary>
    /// CPU affinity group assignment.
    /// </summary>
    CpuAffinity,

    /// <summary>
    /// GPU affinity group assignment.
    /// </summary>
    GpuAffinity,

    /// <summary>
    /// Boost level (0-3).
    /// </summary>
    BoostLevel
}

/// <summary>
/// GPU operating modes controlled by the MLP.
/// </summary>
public enum GpuMode
{
    /// <summary>
    /// Eco mode for power saving.
    /// </summary>
    Eco = 0,

    /// <summary>
    /// Performance mode for balanced operation.
    /// </summary>
    Performance = 1,

    /// <summary>
    /// Turbo mode for increased performance.
    /// </summary>
    Turbo = 2,

    /// <summary>
    /// Maximum performance mode.
    /// </summary>
    Max = 3
}