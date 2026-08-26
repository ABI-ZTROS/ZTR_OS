using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Controls audio-related features for ASUS devices including master mute,
/// microphone mute, and volume control via ACPI and coreaudio.dll.
/// </summary>
public class AudioControl : IDisposable
{
    private readonly AsusAcpi _acpi;
    private readonly ILogger<AudioControl>? _logger;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of the <see cref="AudioControl"/> class.
    /// </summary>
    /// <param name="acpi">The ASUS ACPI interface for hardware communication.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public AudioControl(AsusAcpi acpi, ILogger<AudioControl>? logger = null)
    {
        _acpi = acpi ?? throw new ArgumentNullException(nameof(acpi));
        _logger = logger;
    }

    /// <summary>
    /// Mutes or unmutes the master audio output.
    /// </summary>
    /// <param name="mute">True to mute; false to unmute.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetMasterMute(bool mute)
    {
        try
        {
            _logger?.LogInformation("Setting master mute to {State}", mute ? "muted" : "unmuted");
            bool result = _acpi.DeviceSet(AsusDevice.AudioMute, mute ? 1 : 0, $"SetMasterMute({mute})");

            if (!result)
                _logger?.LogWarning("Failed to set master mute to {State}", mute ? "muted" : "unmuted");

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting master mute");
            return false;
        }
    }

    /// <summary>
    /// Gets the current master mute status.
    /// </summary>
    /// <returns>True if master audio is muted; false otherwise.</returns>
    public bool GetMasterMute()
    {
        try
        {
            return _acpi.DeviceGet(AsusDevice.AudioMute) > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Mutes or unmutes the microphone input.
    /// </summary>
    /// <param name="mute">True to mute; false to unmute.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetMicMute(bool mute)
    {
        try
        {
            _logger?.LogInformation("Setting mic mute to {State}", mute ? "muted" : "unmuted");
            bool result = _acpi.DeviceSet(AsusDevice.MicMute, mute ? 1 : 0, $"SetMicMute({mute})");

            if (!result)
                _logger?.LogWarning("Failed to set mic mute to {State}", mute ? "muted" : "unmuted");

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting mic mute");
            return false;
        }
    }

    /// <summary>
    /// Gets the current microphone mute status.
    /// </summary>
    /// <returns>True if the microphone is muted; false otherwise.</returns>
    public bool GetMicMute()
    {
        try
        {
            return _acpi.DeviceGet(AsusDevice.MicMute) > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the master volume level.
    /// </summary>
    /// <returns>The volume level (0-100), or -1 if unavailable.</returns>
    public int GetMasterVolume()
    {
        try
        {
            return _acpi.DeviceGet(AsusDevice.AudioMute);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading master volume");
            return -1;
        }
    }

    /// <summary>
    /// Sets the master volume level.
    /// </summary>
    /// <param name="volume">The volume level (0-100).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    public bool SetMasterVolume(int volume)
    {
        if (volume < 0 || volume > 100)
        {
            _logger?.LogWarning("Invalid volume level: {Volume}. Must be 0-100.", volume);
            return false;
        }

        try
        {
            _logger?.LogInformation("Setting master volume to {Volume}%", volume);
            bool result = _acpi.DeviceSet(AsusDevice.AudioMute, volume, $"SetMasterVolume({volume})");

            if (!result)
                _logger?.LogWarning("Failed to set master volume to {Volume}%", volume);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting master volume to {Volume}%", volume);
            return false;
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