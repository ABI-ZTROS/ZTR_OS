using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Comprehensive automation engine that monitors system power events
/// (AC/battery) and automatically switches hardware modes based on
/// configurable rules defined in <see cref="AutomationConfig"/>.
/// </summary>
/// <remarks>
/// <para>
/// This service integrates with <see cref="SystemEvents.PowerModeChanged"/>
/// to detect power source transitions and applies corresponding hardware
/// configurations through <see cref="ModeControl"/>, <see cref="GPUModeControl"/>,
/// <see cref="ScreenControl"/>, <see cref="KeyboardControl"/>, and
/// <see cref="BatteryControl"/>.
/// </para>
/// <para>
/// Features include:
/// <list type="bullet">
///   <item>Performance mode auto-switch (e.g., Turbo on AC, Balanced on battery)</item>
///   <item>GPU mode optimization (disable dGPU on battery, enable on AC)</item>
///   <item>Screen refresh rate auto-switch (e.g., 60Hz on battery, max Hz on AC)</item>
///   <item>Keyboard backlight timeout management</item>
///   <item>Battery charge limit auto-apply</item>
/// </list>
/// </para>
/// </remarks>
public class AutomationService : IDisposable
{
    private readonly ModeControl _modeControl;
    private readonly GPUModeControl _gpuModeControl;
    private readonly ScreenControl _screenControl;
    private readonly KeyboardControl _keyboardControl;
    private readonly BatteryControl _batteryControl;
    private readonly ILogger<AutomationService>? _logger;
    private readonly object _lock = new();

    private AutomationConfig _config;
    private bool _isMonitoring;
    private bool _disposed;
    private PowerTrigger _currentPowerState;
    private Timer? _keyboardTimeoutTimer;
    private int _lastAppliedRefreshRate;

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte Reserved1;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus lpSystemPowerStatus);

    /// <summary>
    /// Gets a value indicating whether the automation engine is
    /// currently monitoring power events.
    /// </summary>
    public bool IsMonitoring => _isMonitoring;

    /// <summary>
    /// Gets a value indicating whether the automation engine is enabled
    /// via configuration.
    /// </summary>
    public bool IsEnabled => _config.IsEnabled;

    /// <summary>
    /// Gets the current power source state as determined by the engine.
    /// </summary>
    public PowerTrigger CurrentPowerState => _currentPowerState;

    /// <summary>
    /// Gets or sets the automation configuration. Changing the configuration
    /// while monitoring is active will apply the new rules immediately
    /// if the current power state matches.
    /// </summary>
    public AutomationConfig Config
    {
        get => _config;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            lock (_lock)
            {
                _config = value;
            }
        }
    }

    /// <summary>
    /// Occurs when the automation engine applies a rule in response to
    /// a power mode change.
    /// </summary>
    public event EventHandler<AutomationRuleAppliedEventArgs>? RuleApplied;

    /// <summary>
    /// Occurs when a power mode change is detected by the engine.
    /// </summary>
    public event EventHandler<PowerStateChangedEventArgs>? PowerStateChanged;

    /// <summary>
    /// Creates a new instance of the <see cref="AutomationService"/> class.
    /// </summary>
    /// <param name="modeControl">The mode controller for performance mode switching.</param>
    /// <param name="gpuModeControl">The GPU mode controller for GPU power management.</param>
    /// <param name="screenControl">The screen controller for refresh rate management.</param>
    /// <param name="keyboardControl">The keyboard controller for backlight management.</param>
    /// <param name="batteryControl">The battery controller for charge limit management.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    /// <param name="config">Optional automation configuration. If null, defaults are used.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any required controller is null.
    /// </exception>
    public AutomationService(
        ModeControl modeControl,
        GPUModeControl gpuModeControl,
        ScreenControl screenControl,
        KeyboardControl keyboardControl,
        BatteryControl batteryControl,
        ILogger<AutomationService>? logger = null,
        AutomationConfig? config = null)
    {
        _modeControl = modeControl ?? throw new ArgumentNullException(nameof(modeControl));
        _gpuModeControl = gpuModeControl ?? throw new ArgumentNullException(nameof(gpuModeControl));
        _screenControl = screenControl ?? throw new ArgumentNullException(nameof(screenControl));
        _keyboardControl = keyboardControl ?? throw new ArgumentNullException(nameof(keyboardControl));
        _batteryControl = batteryControl ?? throw new ArgumentNullException(nameof(batteryControl));
        _logger = logger;
        _config = config ?? AutomationConfig.CreateDefault();
        _currentPowerState = GetCurrentPowerState();
    }

    /// <summary>
    /// Starts monitoring system power events and applies the initial
    /// automation rules based on the current power state.
    /// </summary>
    /// <returns>True if monitoring was started successfully; otherwise false.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
    public bool Start()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AutomationService));

        lock (_lock)
        {
            if (_isMonitoring)
            {
                _logger?.LogWarning("Automation service is already monitoring");
                return false;
            }

            if (!_config.IsEnabled)
            {
                _logger?.LogInformation("Automation service is disabled via configuration");
                return false;
            }

            try
            {
                SystemEvents.PowerModeChanged += OnPowerModeChanged;
                _isMonitoring = true;

                _currentPowerState = GetCurrentPowerState();
                _logger?.LogInformation(
                    "Automation service started. Current power state: {State}",
                    _currentPowerState);

                ApplyRulesForState(_currentPowerState);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start automation service");
                return false;
            }
        }
    }

    /// <summary>
    /// Stops monitoring system power events and restores default behavior.
    /// </summary>
    public void Stop()
    {
        if (_disposed) return;

        lock (_lock)
        {
            if (!_isMonitoring)
                return;

            try
            {
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error unsubscribing from power mode events");
            }

            StopKeyboardTimeoutTimer();
            _isMonitoring = false;
            _logger?.LogInformation("Automation service stopped");
        }
    }

    /// <summary>
    /// Immediately applies all automation rules for the current power state.
    /// This can be called to force a re-application of rules after manual
    /// configuration changes.
    /// </summary>
    public void ApplyRulesForCurrentState()
    {
        lock (_lock)
        {
            if (!_isMonitoring)
            {
                _logger?.LogWarning("Cannot apply rules: automation service is not monitoring");
                return;
            }

            ApplyRulesForState(_currentPowerState);
        }
    }

    /// <summary>
    /// Determines the current power state by querying the system via Win32 API.
    /// </summary>
    /// <returns>
    /// <see cref="PowerTrigger.AC"/> if on AC power, <see cref="PowerTrigger.Battery"/> otherwise.
    /// </returns>
    private PowerTrigger GetCurrentPowerState()
    {
        try
        {
            if (GetSystemPowerStatus(out var status))
            {
                return status.ACLineStatus == 1
                    ? PowerTrigger.AC
                    : PowerTrigger.Battery;
            }

            return PowerTrigger.Battery;
        }
        catch
        {
            return PowerTrigger.Battery;
        }
    }

    /// <summary>
    /// Handles the <see cref="SystemEvents.PowerModeChanged"/> event.
    /// Dispatches to the appropriate handler based on the power mode.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments containing the new power mode.</param>
    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (_disposed || !_config.IsEnabled) return;

        switch (e.Mode)
        {
            case PowerModes.AcFullOn:
                HandlePowerTransition(PowerTrigger.AC);
                break;

            case PowerModes.Battery:
                HandlePowerTransition(PowerTrigger.Battery);
                break;

            case PowerModes.Suspend:
                HandleSuspend();
                break;

            case PowerModes.Resume:
                HandleResume();
                break;

            case PowerModes.StatusChange:
                var newState = GetCurrentPowerState();
                if (newState != _currentPowerState)
                {
                    HandlePowerTransition(newState);
                }
                break;
        }
    }

    /// <summary>
    /// Handles a transition between AC and battery power.
    /// Finds and applies the matching automation rule.
    /// </summary>
    /// <param name="newState">The new power state.</param>
    private void HandlePowerTransition(PowerTrigger newState)
    {
        lock (_lock)
        {
            _logger?.LogInformation(
                "Power transition detected: {OldState} -> {NewState}",
                _currentPowerState, newState);

            _currentPowerState = newState;

            PowerStateChanged?.Invoke(this, new PowerStateChangedEventArgs
            {
                NewState = newState,
                Timestamp = DateTime.Now
            });

            ApplyRulesForState(newState);
        }
    }

    /// <summary>
    /// Handles system suspend events by stopping timers and
    /// preserving the current state.
    /// </summary>
    private void HandleSuspend()
    {
        _logger?.LogDebug("System suspend detected, pausing automation timers");
        StopKeyboardTimeoutTimer();
    }

    /// <summary>
    /// Handles system resume events by re-evaluating the power state
    /// and re-applying rules as necessary.
    /// </summary>
    private void HandleResume()
    {
        _logger?.LogInformation("System resume detected, re-evaluating power state");

        var newState = GetCurrentPowerState();
        if (newState != _currentPowerState)
        {
            HandlePowerTransition(newState);
        }
        else
        {
            ApplyRulesForState(_currentPowerState);
        }
    }

    /// <summary>
    /// Applies all automation rules configured for the specified power state.
    /// </summary>
    /// <param name="state">The power trigger state to apply rules for.</param>
    private void ApplyRulesForState(PowerTrigger state)
    {
        var rules = _config.GetAllRulesForTrigger(state);

        if (rules.Count == 0)
        {
            _logger?.LogDebug("No automation rules configured for trigger: {State}", state);
            return;
        }

        _logger?.LogInformation(
            "Applying {Count} automation rule(s) for trigger: {State}",
            rules.Count, state);

        bool anyApplied = false;

        foreach (var rule in rules)
        {
            try
            {
                bool applied = ApplyRule(rule, state);
                if (applied)
                {
                    anyApplied = true;
                    RuleApplied?.Invoke(this, new AutomationRuleAppliedEventArgs
                    {
                        Rule = rule,
                        Trigger = state,
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying automation rule: {Rule}", rule.Name);
            }
        }

        if (!anyApplied)
        {
            _logger?.LogDebug("No rules were successfully applied for trigger: {State}", state);
        }
    }

    /// <summary>
    /// Applies a single automation rule by executing each of its
    /// configured actions.
    /// </summary>
    /// <param name="rule">The rule to apply.</param>
    /// <param name="state">The current power state that triggered the rule.</param>
    /// <returns>True if at least one action was applied successfully.</returns>
    private bool ApplyRule(AutomationRule rule, PowerTrigger state)
    {
        bool anyApplied = false;

        _logger?.LogDebug(
            "Applying rule '{Name}' [{Trigger}]: PerfMode={PerfMode}, GpuMode={GpuMode}, " +
            "RefreshRate={RefreshRate}, KbTimeout={KbTimeout}, ChargeLimit={ChargeLimit}, " +
            "OptimizeGpu={OptimizeGpu}",
            rule.Name, rule.Trigger,
            rule.PerformanceMode?.ToString() ?? "(unchanged)",
            rule.GpuMode?.ToString() ?? "(unchanged)",
            rule.RefreshRate?.ToString() ?? "(unchanged)",
            rule.KeyboardTimeoutSeconds?.ToString() ?? "(unchanged)",
            rule.ChargeLimit?.ToString() ?? "(unchanged)",
            rule.OptimizeGpu);

        if (rule.PerformanceMode.HasValue)
        {
            if (ApplyPerformanceMode(rule.PerformanceMode.Value))
                anyApplied = true;
        }

        if (rule.GpuMode.HasValue)
        {
            if (ApplyGpuMode(rule.GpuMode.Value))
                anyApplied = true;
        }

        if (rule.RefreshRate.HasValue)
        {
            if (ApplyRefreshRate(rule.RefreshRate.Value))
                anyApplied = true;
        }

        if (rule.KeyboardTimeoutSeconds.HasValue)
        {
            if (ApplyKeyboardTimeout(rule.KeyboardTimeoutSeconds.Value, state))
                anyApplied = true;
        }
        else
        {
            if (state == PowerTrigger.AC)
            {
                RestoreKeyboardBacklight();
            }
        }

        if (rule.ChargeLimit.HasValue)
        {
            if (ApplyChargeLimit(rule.ChargeLimit.Value))
                anyApplied = true;
        }

        if (rule.OptimizeGpu)
        {
            if (ApplyOptimizedGpuLogic(state))
                anyApplied = true;
        }

        return anyApplied;
    }

    /// <summary>
    /// Applies the specified performance mode via <see cref="ModeControl"/>.
    /// </summary>
    /// <param name="mode">The performance mode to apply.</param>
    /// <returns>True if the operation succeeded.</returns>
    private bool ApplyPerformanceMode(AsusMode mode)
    {
        try
        {
            _logger?.LogInformation("Applying performance mode: {Mode}", mode);
            bool result = _modeControl.SetMode(mode);
            if (result)
            {
                _logger?.LogInformation("Performance mode set to: {Mode}", mode);
            }
            else
            {
                _logger?.LogWarning("Failed to set performance mode: {Mode}", mode);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying performance mode: {Mode}", mode);
            return false;
        }
    }

    /// <summary>
    /// Applies the specified GPU mode via <see cref="GPUModeControl"/>.
    /// </summary>
    /// <param name="mode">The GPU mode to apply.</param>
    /// <returns>True if the operation succeeded.</returns>
    private bool ApplyGpuMode(AsusGPU mode)
    {
        try
        {
            _logger?.LogInformation("Applying GPU mode: {Mode}", mode);
            bool result = _gpuModeControl.SetGpuMode(mode);
            if (result)
            {
                _logger?.LogInformation("GPU mode set to: {Mode}", mode);
            }
            else
            {
                _logger?.LogWarning("Failed to set GPU mode: {Mode}", mode);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying GPU mode: {Mode}", mode);
            return false;
        }
    }

    /// <summary>
    /// Applies the specified screen refresh rate via <see cref="ScreenControl"/>.
    /// If the requested rate is not supported, the closest supported rate is used.
    /// </summary>
    /// <param name="rate">The refresh rate in Hz to apply.</param>
    /// <returns>True if the operation succeeded.</returns>
    private bool ApplyRefreshRate(int rate)
    {
        try
        {
            var supportedRates = _screenControl.GetSupportedRefreshRates();
            int targetRate = rate;

            if (!supportedRates.Contains(rate))
            {
                targetRate = supportedRates
                    .OrderBy(r => Math.Abs(r - rate))
                    .First();

                _logger?.LogWarning(
                    "Requested refresh rate {Requested}Hz is not supported. Using closest supported rate: {Actual}Hz",
                    rate, targetRate);
            }

            _logger?.LogInformation("Applying screen refresh rate: {Rate}Hz", targetRate);
            bool result = _screenControl.SetRefreshRate(targetRate);
            if (result)
            {
                _lastAppliedRefreshRate = targetRate;
                _logger?.LogInformation("Refresh rate set to: {Rate}Hz", targetRate);
            }
            else
            {
                _logger?.LogWarning("Failed to set refresh rate: {Rate}Hz", targetRate);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying refresh rate: {Rate}Hz", rate);
            return false;
        }
    }

    /// <summary>
    /// Applies keyboard backlight timeout management. On battery power,
    /// starts a countdown timer that turns off the backlight when it expires.
    /// On AC power, restores the backlight to maximum brightness.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout duration in seconds.</param>
    /// <param name="state">The current power state.</param>
    /// <returns>True if the operation was handled successfully.</returns>
    private bool ApplyKeyboardTimeout(int timeoutSeconds, PowerTrigger state)
    {
        try
        {
            StopKeyboardTimeoutTimer();

            if (state == PowerTrigger.Battery)
            {
                if (timeoutSeconds <= 0)
                {
                    _logger?.LogInformation("Battery keyboard timeout is 0, turning backlight off immediately");
                    return _keyboardControl.TurnOffBacklight();
                }

                _logger?.LogInformation(
                    "Battery keyboard timeout set to {Seconds}s, backlight will turn off after this period",
                    timeoutSeconds);

                _keyboardTimeoutTimer = new Timer(
                    _ =>
                    {
                        _logger?.LogInformation(
                            "Keyboard backlight timeout expired, turning off backlight");
                        _keyboardControl.TurnOffBacklight();
                    },
                    null,
                    TimeSpan.FromSeconds(timeoutSeconds),
                    Timeout.InfiniteTimeSpan);

                return true;
            }
            else
            {
                _logger?.LogInformation("AC power: restoring keyboard backlight to maximum");
                return RestoreKeyboardBacklight();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying keyboard timeout: {Seconds}s", timeoutSeconds);
            return false;
        }
    }

    /// <summary>
    /// Applies the battery charge limit via <see cref="BatteryControl"/>.
    /// </summary>
    /// <param name="percent">The charge limit percentage (60, 80, or 100).</param>
    /// <returns>True if the operation succeeded.</returns>
    private bool ApplyChargeLimit(int percent)
    {
        try
        {
            _logger?.LogInformation("Applying battery charge limit: {Percent}%", percent);
            bool result = _batteryControl.SetChargeLimit(percent);
            if (result)
            {
                _logger?.LogInformation("Charge limit set to: {Percent}%", percent);
            }
            else
            {
                _logger?.LogWarning("Failed to set charge limit: {Percent}%", percent);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying charge limit: {Percent}%", percent);
            return false;
        }
    }

    /// <summary>
    /// Applies optimized GPU logic based on the power state.
    /// On battery: switches GPU to Eco mode and disables MUX to save power.
    /// On AC: switches GPU to Ultimate mode and enables MUX for performance.
    /// </summary>
    /// <param name="state">The current power state.</param>
    /// <returns>True if at least one operation succeeded.</returns>
    private bool ApplyOptimizedGpuLogic(PowerTrigger state)
    {
        try
        {
            if (state == PowerTrigger.Battery)
            {
                _logger?.LogInformation(
                    "Optimized GPU logic (battery): switching to Eco mode and disabling MUX");

                bool gpuResult = _gpuModeControl.SetGpuMode(AsusGPU.Eco);
                bool muxResult = _gpuModeControl.SetMux(false);

                _logger?.LogDebug("GPU Eco mode: {GpuResult}, MUX disabled: {MuxResult}",
                    gpuResult ? "success" : "failed",
                    muxResult ? "success" : "failed");

                return gpuResult || muxResult;
            }
            else
            {
                _logger?.LogInformation(
                    "Optimized GPU logic (AC): switching to Ultimate mode and enabling MUX");

                bool gpuResult = _gpuModeControl.SetGpuMode(AsusGPU.Ultimate);
                bool muxResult = _gpuModeControl.SetMux(true);

                _logger?.LogDebug("GPU Ultimate mode: {GpuResult}, MUX enabled: {MuxResult}",
                    gpuResult ? "success" : "failed",
                    muxResult ? "success" : "failed");

                return gpuResult || muxResult;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying optimized GPU logic for state: {State}", state);
            return false;
        }
    }

    /// <summary>
    /// Restores the keyboard backlight to maximum brightness (level 3).
    /// </summary>
    /// <returns>True if the operation succeeded.</returns>
    private bool RestoreKeyboardBacklight()
    {
        try
        {
            return _keyboardControl.SetMaxBrightness();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error restoring keyboard backlight");
            return false;
        }
    }

    /// <summary>
    /// Stops and disposes the keyboard timeout timer if active.
    /// </summary>
    private void StopKeyboardTimeoutTimer()
    {
        if (_keyboardTimeoutTimer != null)
        {
            try
            {
                _keyboardTimeoutTimer.Dispose();
            }
            catch
            {
            }

            _keyboardTimeoutTimer = null;
        }
    }

    /// <summary>
    /// Logs the current state of all controlled hardware for diagnostics.
    /// </summary>
    public void LogCurrentState()
    {
        try
        {
            _logger?.LogInformation("=== Automation State Snapshot ===");
            _logger?.LogInformation("Power State: {State}", _currentPowerState);
            _logger?.LogInformation("Monitoring: {IsMonitoring}", _isMonitoring);
            _logger?.LogInformation("Enabled: {IsEnabled}", _config.IsEnabled);
            _logger?.LogInformation("Rules Count: {Count}", _config.Rules.Count);

            try
            {
                _logger?.LogInformation("Performance Mode: {Mode}", _modeControl.CurrentMode);
            }
            catch
            {
                _logger?.LogWarning("Performance Mode: (unavailable)");
            }

            try
            {
                _logger?.LogInformation("GPU Mode: {Mode}", _gpuModeControl.GetGpuMode());
            }
            catch
            {
                _logger?.LogWarning("GPU Mode: (unavailable)");
            }

            try
            {
                _logger?.LogInformation("Refresh Rate: {Rate}Hz", _screenControl.GetCurrentRefreshRate());
            }
            catch
            {
                _logger?.LogWarning("Refresh Rate: (unavailable)");
            }

            try
            {
                _logger?.LogInformation("Keyboard Brightness: {Level}", _keyboardControl.GetBrightness());
            }
            catch
            {
                _logger?.LogWarning("Keyboard Brightness: (unavailable)");
            }

            try
            {
                _logger?.LogInformation("Charge Limit: {Limit}%", _batteryControl.GetChargeLimit());
            }
            catch
            {
                _logger?.LogWarning("Charge Limit: (unavailable)");
            }

            _logger?.LogInformation("=== End State Snapshot ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error logging automation state");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">True when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            try
            {
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            }
            catch
            {
            }

            StopKeyboardTimeoutTimer();
            _isMonitoring = false;
        }

        _disposed = true;
    }
}

/// <summary>
/// Provides data for the <see cref="AutomationService.RuleApplied"/> event.
/// </summary>
public class AutomationRuleAppliedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the rule that was applied.
    /// </summary>
    public AutomationRule Rule { get; set; } = default!;

    /// <summary>
    /// Gets or sets the power trigger that caused the rule to be applied.
    /// </summary>
    public PowerTrigger Trigger { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the rule was applied.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Provides data for the <see cref="AutomationService.PowerStateChanged"/> event.
/// </summary>
public class PowerStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the new power state.
    /// </summary>
    public PowerTrigger NewState { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the state change was detected.
    /// </summary>
    public DateTime Timestamp { get; set; }
}