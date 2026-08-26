namespace ZTR.Intelligence;

/// <summary>
/// Provides manual override capability to disable MLP-based scheduling
/// and use user-specified hardware settings instead. Priority:
/// Manual override > MLP auto-scheduling > system defaults.
/// </summary>
public class ManualOverride
{
    private ManualSettings _settings;
    private bool _isActive;

    /// <summary>
    /// Gets whether the manual override is currently active.
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    /// Gets the current manual settings.
    /// </summary>
    public ManualSettings CurrentSettings => _settings;

    /// <summary>
    /// Event raised when the override state changes.
    /// </summary>
    public event EventHandler<OverrideStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Creates a new instance of the <see cref="ManualOverride"/> class
    /// with default hardware settings.
    /// </summary>
    public ManualOverride()
    {
        _settings = new ManualSettings();
        _isActive = false;
    }

    /// <summary>
    /// Creates a new instance with specific initial settings.
    /// </summary>
    /// <param name="settings">Initial manual settings.</param>
    public ManualOverride(ManualSettings settings)
    {
        _settings = settings;
        _isActive = false;
    }

    /// <summary>
    /// Enables the manual override with the current settings.
    /// When active, MLP scheduling decisions are ignored.
    /// </summary>
    public void Enable()
    {
        if (!_isActive)
        {
            _isActive = true;
            StateChanged?.Invoke(this, new OverrideStateChangedEventArgs(true, _settings));
        }
    }

    /// <summary>
    /// Enables the manual override with new settings.
    /// </summary>
    /// <param name="settings">The manual settings to apply.</param>
    public void Enable(ManualSettings settings)
    {
        _settings = settings;
        if (!_isActive)
        {
            _isActive = true;
            StateChanged?.Invoke(this, new OverrideStateChangedEventArgs(true, _settings));
        }
    }

    /// <summary>
    /// Disables the manual override and returns control to the MLP scheduler.
    /// </summary>
    public void Disable()
    {
        if (_isActive)
        {
            _isActive = false;
            StateChanged?.Invoke(this, new OverrideStateChangedEventArgs(false, _settings));
        }
    }

    /// <summary>
    /// Updates the manual settings without changing the active state.
    /// </summary>
    /// <param name="settings">New settings to apply.</param>
    public void UpdateSettings(ManualSettings settings)
    {
        _settings = settings;
        if (_isActive)
        {
            StateChanged?.Invoke(this, new OverrideStateChangedEventArgs(true, _settings));
        }
    }

    /// <summary>
    /// Gets the current manually-overridden hardware settings.
    /// </summary>
    /// <returns>The active manual settings.</returns>
    public ManualSettings GetCurrentSettings()
    {
        return _settings;
    }

    /// <summary>
    /// Determines whether MLP scheduling should be used.
    /// Returns false when manual override is active.
    /// </summary>
    /// <returns>True if MLP should be bypassed.</returns>
    public bool ShouldUseMlp() => !_isActive;
}

/// <summary>
/// Represents manually-configured hardware settings that override MLP decisions.
/// </summary>
public class ManualSettings
{
    /// <summary>
    /// Gets or sets the manual SPL value in watts.
    /// </summary>
    public int SplWatts { get; set; } = 45;

    /// <summary>
    /// Gets or sets the manual fan curve offset (0-100%).
    /// </summary>
    public int FanCurveOffset { get; set; } = 50;

    /// <summary>
    /// Gets or sets the manual GPU clock offset in MHz.
    /// </summary>
    public int GpuClockOffset { get; set; } = 0;

    /// <summary>
    /// Gets or sets the manual CPU clock offset in MHz.
    /// </summary>
    public int CpuClockOffset { get; set; } = 0;

    /// <summary>
    /// Gets or sets the manual GPU mode.
    /// </summary>
    public GpuMode GpuMode { get; set; } = GpuMode.Performance;

    /// <summary>
    /// Gets or sets the manual CPU affinity group (0-3).
    /// </summary>
    public int CpuAffinity { get; set; } = 0;

    /// <summary>
    /// Gets or sets the manual GPU affinity group (0-3).
    /// </summary>
    public int GpuAffinity { get; set; } = 0;

    /// <summary>
    /// Gets or sets the manual boost level (0-3).
    /// </summary>
    public int BoostLevel { get; set; } = 1;
}

/// <summary>
/// Provides data for the <see cref="ManualOverride.StateChanged"/> event.
/// </summary>
public class OverrideStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets whether the override is now active.
    /// </summary>
    public bool IsActive { get; }

    /// <summary>
    /// Gets the current settings at the time of change.
    /// </summary>
    public ManualSettings Settings { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="OverrideStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="isActive">Whether the override is now active.</param>
    /// <param name="settings">The current manual settings.</param>
    public OverrideStateChangedEventArgs(bool isActive, ManualSettings settings)
    {
        IsActive = isActive;
        Settings = settings;
    }
}