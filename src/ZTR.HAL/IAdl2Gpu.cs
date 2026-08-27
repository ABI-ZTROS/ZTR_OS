namespace ZTR.HAL;

/// <summary>
/// Abstracts AMD ADL2 (AMD Display Library) hardware operations to enable unit testing without GPU hardware.
/// </summary>
public interface IAdl2Gpu : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the ADL2 library is available and initialized.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the number of AMD GPUs detected.
    /// </summary>
    int GpuCount { get; }

    /// <summary>
    /// Gets the GPU name for the specified index.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <returns>The GPU name, or null if unavailable.</returns>
    string? GetGpuName(int index);

    /// <summary>
    /// Gets the current temperature for the specified GPU.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <returns>The temperature in Celsius, or null if unavailable.</returns>
    int? GetTemperature(int index);

    /// <summary>
    /// Gets the hotspot temperature for the specified GPU.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <returns>The hotspot temperature in Celsius, or null if unavailable.</returns>
    int? GetHotspotTemperature(int index);

    /// <summary>
    /// Gets the GPU utilization percentage.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <returns>The utilization percentage (0-100), or null if unavailable.</returns>
    int? GetUsage(int index);

    /// <summary>
    /// Gets VRAM information.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <returns>A tuple of (usedMb, totalMb), or null if unavailable.</returns>
    (long usedMb, long totalMb)? GetVramInfo(int index);

    /// <summary>
    /// Gets the current GPU power draw in watts.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <returns>The power draw in watts, or null if unavailable.</returns>
    float? GetPower(int index);

    /// <summary>
    /// Gets the current clock speeds.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <returns>A tuple of (coreClockMHz, memoryClockMHz), or null if unavailable.</returns>
    (int coreClockMHz, int memoryClockMHz)? GetClockInfo(int index);

    /// <summary>
    /// Gets the current fan speed as a percentage.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <returns>The fan speed percentage (0-100), or null if unavailable.</returns>
    int? GetFanSpeed(int index);

    /// <summary>
    /// Sets core and memory clock offsets.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <param name="coreOffset">The core clock offset in MHz.</param>
    /// <param name="memoryOffset">The memory clock offset in MHz.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    bool SetClocks(int index, int coreOffset, int memoryOffset);

    /// <summary>
    /// Resets all clock offsets to default values.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    bool ResetClocks(int index);

    /// <summary>
    /// Sets the power limit in watts.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <param name="powerLimit">The power limit in watts.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    bool SetPowerLimit(int index, int powerLimit);

    /// <summary>
    /// Gets the power limit range.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <returns>A tuple of (minWatts, maxWatts), or null if unavailable.</returns>
    (int minWatts, int maxWatts)? GetPowerLimitRange(int index);

    /// <summary>
    /// Sets the FPS limit for the GPU.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <param name="fps">The FPS limit value, or 0 to disable.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    bool SetFPSLimit(int index, int fps);

    /// <summary>
    /// Gets the current FPS limit.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <returns>The FPS limit value, or null if not set.</returns>
    int? GetFPSLimit(int index);

    /// <summary>
    /// Sets the iGPU power level.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <param name="power">The power level in milliwatts.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    bool SetiGpuPower(int index, int power);

    /// <summary>
    /// Gets iGPU-specific sensor readings.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <returns>A dictionary of sensor name to value, or null if unavailable.</returns>
    IReadOnlyDictionary<string, double>? GetiGpuSensors(int index);

    /// <summary>
    /// Sets the GPU fan speed manually.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    /// <param name="speed">The fan speed percentage (0-100).</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    bool SetFanSpeed(int index, int speed);

    /// <summary>
    /// Kills all processes currently using the GPU.
    /// </summary>
    /// <param name="index">The GPU index.</param>
    void KillGpuApps(int index);
}