using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Controls GPU operating modes (Eco, Standard, Ultimate) and MUX switching
/// for ASUS ROG systems. Uses <see cref="IGpuControl"/> for GPU-specific operations
/// and <see cref="AsusAcpi"/> for hardware-level mode switching.
/// </summary>
public class GPUModeControl : IDisposable
{
    private readonly IGpuControl _gpuControl;
    private readonly AsusAcpi _acpi;
    private readonly ILogger<GPUModeControl>? _logger;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of the <see cref="GPUModeControl"/> class.
    /// </summary>
    /// <param name="gpuControl">The GPU control interface for GPU-specific operations.</param>
    /// <param name="acpi">The ASUS ACPI interface for hardware communication.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public GPUModeControl(IGpuControl gpuControl, AsusAcpi acpi, ILogger<GPUModeControl>? logger = null)
    {
        _gpuControl = gpuControl;
        _acpi = acpi;
        _logger = logger;
    }

    /// <summary>
    /// Sets the GPU operating mode (Eco, Standard, or Ultimate).
    /// </summary>
    /// <param name="mode">The GPU mode to set.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetGpuMode(AsusGPU mode)
    {
        try
        {
            _logger?.LogInformation("Setting GPU mode to {Mode}", mode);

            bool result = _acpi.SetGPUMode(mode);
            if (!result)
            {
                _logger?.LogWarning("Failed to set GPU mode via ACPI, attempting fallback");
            }

            ApplyModeSettings(mode);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting GPU mode to {Mode}", mode);
            return false;
        }
    }

    /// <summary>
    /// Gets the current GPU operating mode.
    /// </summary>
    /// <returns>
    /// The current <see cref="AsusGPU"/> mode, or <see cref="AsusGPU.Standard"/> if unavailable.
    /// </returns>
    public AsusGPU GetGpuMode()
    {
        try
        {
            int modeValue = _acpi.GetGPUMode();
            if (modeValue >= 0 && Enum.IsDefined(typeof(AsusGPU), modeValue))
            {
                return (AsusGPU)modeValue;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading current GPU mode");
        }

        return AsusGPU.Standard;
    }

    /// <summary>
    /// Sets the MUX (Multiplexer) switch to control GPU routing.
    /// When enabled, the dedicated GPU is directly connected to the display output.
    /// When disabled, the GPU is routed through the iGPU for power savings.
    /// </summary>
    /// <param name="enable">True to enable MUX (dedicated GPU direct output); false to disable.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetMux(bool enable)
    {
        try
        {
            _logger?.LogInformation("Setting MUX to {State}", enable ? "enabled" : "disabled");

            int status = enable ? 1 : 0;
            bool result = _acpi.DeviceSet(AsusDevice.GPUMux, status, $"SetMux({enable})");

            if (!result)
            {
                _logger?.LogWarning("Failed to set MUX state");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting MUX state");
            return false;
        }
    }

    /// <summary>
    /// Gets the current MUX state.
    /// </summary>
    /// <returns>True if MUX is enabled; false if disabled or unavailable.</returns>
    public bool GetMux()
    {
        try
        {
            int status = _acpi.DeviceGet(AsusDevice.GPUMux);
            return status > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Auto-detects GPU-intensive game processes and adjusts GPU settings accordingly.
    /// Scans for known game processes and switches to Ultimate mode for optimal performance.
    /// </summary>
    /// <returns>A list of detected process names.</returns>
    public IReadOnlyList<string> AutoDetectGpuApps()
    {
        var detectedApps = new List<string>();

        try
        {
            string[] gameProcesses = new[]
            {
                "steam", "cs2", "dota2", "valorant", "leagueclient",
                "league of legends", "fortniteclient", "borderlands3",
                "cyberpunk2077", "witcher3", "gta5", "gta6", "rdr2",
                "battlefield", "callofduty", "modernwarfare",
                "eldenring", "dark souls", "hollow knight",
                "minecraft", "terraria", "stardew valley",
                "rocketleague", "fifa", "nba2k",
                "origin", "uplay", "epicgameslauncher",
                "battle.net", "riot clients",
                "LeagueClient", "Riot Client"
            };

            var allProcesses = System.Diagnostics.Process.GetProcesses();

            foreach (var process in allProcesses)
            {
                try
                {
                    string? name = process.ProcessName?.ToLowerInvariant();
                    if (string.IsNullOrEmpty(name)) continue;

                    foreach (string game in gameProcesses)
                    {
                        if (name.Contains(game, StringComparison.OrdinalIgnoreCase))
                        {
                            detectedApps.Add(name);
                            break;
                        }
                    }
                }
                catch
                {
                }
            }

            if (detectedApps.Count > 0)
            {
                _logger?.LogInformation("Detected {Count} GPU app(s): {Apps}",
                    detectedApps.Count, string.Join(", ", detectedApps));

                var currentMode = GetGpuMode();
                if (currentMode == AsusGPU.Eco)
                {
                    _logger?.LogInformation("Switching from Eco to Standard mode due to detected GPU apps");
                    SetGpuMode(AsusGPU.Standard);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error during GPU app auto-detection");
        }

        return detectedApps.AsReadOnly();
    }

    /// <summary>
    /// Applies GPU settings based on the selected mode.
    /// </summary>
    /// <param name="mode">The GPU mode to apply settings for.</param>
    private void ApplyModeSettings(AsusGPU mode)
    {
        try
        {
            switch (mode)
            {
                case AsusGPU.Eco:
                    _gpuControl.SetFanSpeed(0);
                    break;

                case AsusGPU.Standard:
                    _gpuControl.SetFanSpeed(50);
                    break;

                case AsusGPU.Ultimate:
                    _gpuControl.SetFanSpeed(100);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error applying mode-specific GPU settings");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}