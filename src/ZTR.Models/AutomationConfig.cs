namespace ZTR.Models;

/// <summary>
/// Represents the power source trigger for an automation rule.
/// </summary>
public enum PowerTrigger
{
    /// <summary>
    /// Rule triggers when the system is on AC (mains) power.
    /// </summary>
    AC = 0,

    /// <summary>
    /// Rule triggers when the system is on battery power.
    /// </summary>
    Battery = 1
}

/// <summary>
/// Defines a single automation rule that maps a power source trigger
/// to a set of hardware configuration changes.
/// </summary>
public class AutomationRule
{
    /// <summary>
    /// Gets or sets the power source that triggers this rule.
    /// </summary>
    public PowerTrigger Trigger { get; set; }

    /// <summary>
    /// Gets or sets the performance mode to apply when this rule activates.
    /// Null means no change to the current performance mode.
    /// </summary>
    public AsusMode? PerformanceMode { get; set; }

    /// <summary>
    /// Gets or sets the GPU mode to apply when this rule activates.
    /// Null means no change to the current GPU mode.
    /// </summary>
    public AsusGPU? GpuMode { get; set; }

    /// <summary>
    /// Gets or sets the screen refresh rate in Hz to apply.
    /// Null means no change to the current refresh rate.
    /// </summary>
    public int? RefreshRate { get; set; }

    /// <summary>
    /// Gets or sets the keyboard backlight timeout in seconds.
    /// When on battery, the keyboard backlight turns off after this duration.
    /// Set to 0 to immediately turn off, null to leave unchanged.
    /// </summary>
    public int? KeyboardTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the battery charge limit percentage (60, 80, or 100).
    /// Null means no change to the current charge limit.
    /// </summary>
    public int? ChargeLimit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this rule also applies
    /// optimized GPU logic (disable dGPU on battery, enable on AC).
    /// </summary>
    public bool OptimizeGpu { get; set; }

    /// <summary>
    /// Gets or sets a descriptive name for this rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Configuration model for the automation engine.
/// Holds a collection of <see cref="AutomationRule"/> entries that define
/// how the system should respond to power source changes.
/// </summary>
public class AutomationConfig
{
    private readonly List<AutomationRule> _rules = new();

    /// <summary>
    /// Gets or sets a value indicating whether the automation engine is enabled.
    /// When disabled, no automatic mode switching occurs.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default refresh rate to use on AC power.
    /// </summary>
    public int DefaultAcRefreshRate { get; set; } = 144;

    /// <summary>
    /// Gets or sets the default refresh rate to use on battery power.
    /// </summary>
    public int DefaultBatteryRefreshRate { get; set; } = 60;

    /// <summary>
    /// Gets or sets the default keyboard backlight timeout on battery power in seconds.
    /// </summary>
    public int DefaultKeyboardTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets the read-only list of configured automation rules.
    /// </summary>
    public IReadOnlyList<AutomationRule> Rules => _rules.AsReadOnly();

    /// <summary>
    /// Adds a new automation rule to the configuration.
    /// </summary>
    /// <param name="rule">The automation rule to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/> is null.</exception>
    public void AddRule(AutomationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules.Add(rule);
    }

    /// <summary>
    /// Adds a new automation rule with the specified trigger and settings.
    /// </summary>
    /// <param name="trigger">The power source trigger.</param>
    /// <param name="performanceMode">The performance mode to apply, or null for no change.</param>
    /// <param name="gpuMode">The GPU mode to apply, or null for no change.</param>
    /// <param name="refreshRate">The refresh rate in Hz, or null for no change.</param>
    /// <param name="keyboardTimeoutSeconds">The keyboard timeout in seconds, or null for no change.</param>
    /// <param name="chargeLimit">The charge limit percentage (60/80/100), or null for no change.</param>
    /// <param name="optimizeGpu">Whether to apply optimized GPU logic.</param>
    /// <param name="name">Optional descriptive name for the rule.</param>
    public void AddRule(
        PowerTrigger trigger,
        AsusMode? performanceMode = null,
        AsusGPU? gpuMode = null,
        int? refreshRate = null,
        int? keyboardTimeoutSeconds = null,
        int? chargeLimit = null,
        bool optimizeGpu = false,
        string name = "")
    {
        _rules.Add(new AutomationRule
        {
            Trigger = trigger,
            PerformanceMode = performanceMode,
            GpuMode = gpuMode,
            RefreshRate = refreshRate,
            KeyboardTimeoutSeconds = keyboardTimeoutSeconds,
            ChargeLimit = chargeLimit,
            OptimizeGpu = optimizeGpu,
            Name = name
        });
    }

    /// <summary>
    /// Removes the specified automation rule from the configuration.
    /// </summary>
    /// <param name="rule">The rule to remove.</param>
    /// <returns>True if the rule was found and removed; otherwise false.</returns>
    public bool RemoveRule(AutomationRule rule)
    {
        return _rules.Remove(rule);
    }

    /// <summary>
    /// Removes all automation rules that match the specified trigger.
    /// </summary>
    /// <param name="trigger">The power trigger whose rules should be removed.</param>
    /// <returns>The number of rules removed.</returns>
    public int RemoveRulesByTrigger(PowerTrigger trigger)
    {
        int count = _rules.RemoveAll(r => r.Trigger == trigger);
        return count;
    }

    /// <summary>
    /// Gets the automation rule configured for the specified power trigger.
    /// Returns the first matching rule, or null if no rule is configured.
    /// </summary>
    /// <param name="trigger">The power trigger to look up.</param>
    /// <returns>The matching <see cref="AutomationRule"/>, or null if not found.</returns>
    public AutomationRule? GetRuleForTrigger(PowerTrigger trigger)
    {
        return _rules.FirstOrDefault(r => r.Trigger == trigger);
    }

    /// <summary>
    /// Gets all automation rules configured for the specified power trigger.
    /// </summary>
    /// <param name="trigger">The power trigger to look up.</param>
    /// <returns>A read-only list of matching <see cref="AutomationRule"/> instances.</returns>
    public IReadOnlyList<AutomationRule> GetAllRulesForTrigger(PowerTrigger trigger)
    {
        return _rules.Where(r => r.Trigger == trigger).ToList().AsReadOnly();
    }

    /// <summary>
    /// Removes all configured rules.
    /// </summary>
    public void ClearRules()
    {
        _rules.Clear();
    }

    /// <summary>
    /// Creates a default configuration with sensible rules for both
    /// AC and battery power scenarios.
    /// </summary>
    /// <returns>A new <see cref="AutomationConfig"/> with default rules.</returns>
    public static AutomationConfig CreateDefault()
    {
        var config = new AutomationConfig();

        config.AddRule(
            trigger: PowerTrigger.AC,
            performanceMode: AsusMode.PerformanceTurbo,
            gpuMode: AsusGPU.Ultimate,
            refreshRate: 144,
            keyboardTimeoutSeconds: null,
            chargeLimit: 100,
            optimizeGpu: true,
            name: "AC Power - Performance");

        config.AddRule(
            trigger: PowerTrigger.Battery,
            performanceMode: AsusMode.PerformanceBalanced,
            gpuMode: AsusGPU.Eco,
            refreshRate: 60,
            keyboardTimeoutSeconds: 30,
            chargeLimit: 80,
            optimizeGpu: true,
            name: "Battery - Power Saving");

        return config;
    }
}