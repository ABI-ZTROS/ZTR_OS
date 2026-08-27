using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Factory and aggregation service for GPU sensor management.
/// Detects GPU hardware, creates appropriate <see cref="IGpuControl"/> instances,
/// and provides multi-GPU sensor data aggregation.
/// </summary>
public class GpuSensorService : IDisposable
{
    private readonly ILogger<GpuSensorService>? _logger;
    private readonly List<IGpuControl> _gpuControls = new();
    private bool _disposed;

    /// <summary>
    /// Gets the list of detected GPU controls.
    /// </summary>
    public IReadOnlyList<IGpuControl> GpuControls => _gpuControls.AsReadOnly();

    /// <summary>
    /// Creates a new instance of the <see cref="GpuSensorService"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public GpuSensorService(ILogger<GpuSensorService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the GPU sensor service by detecting all available GPUs.
    /// </summary>
    /// <returns>The number of GPUs detected.</returns>
    public int Initialize()
    {
        _gpuControls.Clear();

        _logger?.LogInformation("Starting GPU detection...");

        DetectNvidiaGpus();
        DetectAmdGpus();

        _logger?.LogInformation("GPU detection complete. Found {Count} GPU(s)", _gpuControls.Count);

        return _gpuControls.Count;
    }

    /// <summary>
    /// Creates a GPU control for the specified GPU type and index.
    /// </summary>
    /// <param name="isNvidia">True for NVIDIA, false for AMD.</param>
    /// <param name="gpuIndex">The zero-based GPU index.</param>
    /// <returns>An <see cref="IGpuControl"/> instance, or null if creation failed.</returns>
    public IGpuControl? CreateGpuControl(bool isNvidia, int gpuIndex = 0)
    {
        try
        {
            if (isNvidia)
            {
                var control = new NvidiaGpuControl(gpuIndex);
                if (control.IsValid)
                {
                    _logger?.LogInformation("Created NVIDIA GPU control for index {Index}: {Name}",
                        gpuIndex, control.FullName);
                    return control;
                }

                _logger?.LogWarning("NVIDIA GPU at index {Index} is not valid", gpuIndex);
                return null;
            }
            else
            {
                var control = new AmdGpuControl(gpuIndex);
                if (control.IsValid)
                {
                    _logger?.LogInformation("Created AMD GPU control for index {Index}: {Name}",
                        gpuIndex, control.FullName);
                    return control;
                }

                _logger?.LogWarning("AMD GPU at index {Index} is not valid", gpuIndex);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create GPU control for type {Type} index {Index}",
                isNvidia ? "NVIDIA" : "AMD", gpuIndex);
            return null;
        }
    }

    /// <summary>
    /// Aggregates sensor data from all detected GPUs.
    /// </summary>
    /// <returns>A list of sensor reading dictionaries, one per GPU.</returns>
    public IReadOnlyList<GpuState> GetAllGpuStates()
    {
        var states = new List<GpuState>();
        foreach (var gpu in _gpuControls)
        {
            try
            {
                states.Add(gpu.GetState());
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to get state for GPU {Name}", gpu.FullName);
                states.Add(new GpuState());
            }
        }
        return states.AsReadOnly();
    }

    /// <summary>
    /// Gets the primary GPU state (first GPU in the list).
    /// </summary>
    /// <returns>The primary GPU state, or an empty state if no GPUs are detected.</returns>
    public GpuState GetPrimaryGpuState()
    {
        if (_gpuControls.Count > 0)
        {
            try
            {
                return _gpuControls[0].GetState();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to get primary GPU state");
            }
        }
        return new GpuState();
    }

    /// <summary>
    /// Gets the total GPU power consumption across all GPUs.
    /// </summary>
    /// <returns>The total power in watts.</returns>
    public float GetTotalPower()
    {
        float total = 0;
        foreach (var gpu in _gpuControls)
        {
            try
            {
                var power = gpu.GetGpuPower();
                if (power.HasValue)
                    total += power.Value;
            }
            catch
            {
            }
        }
        return total;
    }

    /// <summary>
    /// Gets the highest GPU temperature across all GPUs.
    /// </summary>
    /// <returns>The maximum temperature in Celsius, or 0 if unavailable.</returns>
    public int GetMaxTemperature()
    {
        int max = 0;
        foreach (var gpu in _gpuControls)
        {
            try
            {
                var temp = gpu.GetCurrentTemperature();
                if (temp.HasValue && temp.Value > max)
                    max = temp.Value;
            }
            catch
            {
            }
        }
        return max;
    }

    private void DetectNvidiaGpus()
    {
        try
        {
            // P1c FIXED: Remove 'using' - probe is now owned by _gpuControls list.
            // Previously 'using var' disposed the probe at end of block while keeping
            // a reference in _gpuControls → use-after-free when later accessing the GPU.
            var probe = new NvidiaGpuControl(0);
            if (probe.IsValid)
            {
                _gpuControls.Add(probe);
                _logger?.LogInformation("Detected NVIDIA GPU: {Name}", probe.FullName);
            }
            else
            {
                probe.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "NVIDIA GPU detection failed");
        }
    }

    private void DetectAmdGpus()
    {
        try
        {
            // P1c FIXED: Same fix as DetectNvidiaGpus
            var probe = new AmdGpuControl(0);
            if (probe.IsValid)
            {
                _gpuControls.Add(probe);
                _logger?.LogInformation("Detected AMD GPU: {Name}", probe.FullName);
            }
            else
            {
                probe.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "AMD GPU detection failed");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            foreach (var gpu in _gpuControls)
            {
                try
                {
                    gpu.Dispose();
                }
                catch
                {
                }
            }
            _gpuControls.Clear();
        }
    }
}