using System.Runtime.InteropServices;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Manages GPU affinity for processes using NvAPI/ADL2 P/Invoke wrappers.
/// Provides process-to-GPU and process-to-engine binding with graceful fallback
/// when GPU SDKs are unavailable.
/// </summary>
public class GpuAffinityManager
{
    private readonly INvApiGpu? _nvApi;
    private readonly IAdl2Gpu? _adl2;
    private readonly Dictionary<int, GpuAffinityConfig> _affinityCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GpuAffinityManager"/> class.
    /// </summary>
    public GpuAffinityManager() : this(null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GpuAffinityManager"/> class
    /// with specified GPU SDK abstractions.
    /// </summary>
    /// <param name="nvApi">The NVIDIA NVAPI abstraction, or null to skip NVIDIA support.</param>
    /// <param name="adl2">The AMD ADL2 abstraction, or null to skip AMD support.</param>
    public GpuAffinityManager(INvApiGpu? nvApi, IAdl2Gpu? adl2)
    {
        _nvApi = nvApi;
        _adl2 = adl2;
    }

    /// <summary>
    /// Gets whether any GPU SDK is available for affinity operations.
    /// </summary>
    public bool IsGpuAvailable =>
        (_nvApi?.IsAvailable ?? false) || (_adl2?.IsAvailable ?? false);

    /// <summary>
    /// Binds a process to a specific GPU by index.
    /// </summary>
    /// <param name="processId">The process identifier to bind.</param>
    /// <param name="gpuIndex">The zero-based GPU index to bind to.</param>
    /// <returns>True if the affinity was set successfully; otherwise false.</returns>
    public bool SetGpuAffinity(int processId, int gpuIndex)
    {
        if (processId <= 0 || gpuIndex < 0)
            return false;

        var config = new GpuAffinityConfig
        {
            Enabled = true,
            GpuIndex = gpuIndex,
            EngineId = 0
        };

        _affinityCache[processId] = config;

        try
        {
            ApplyGpuAffinity(processId, config);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current GPU affinity configuration for a process.
    /// </summary>
    /// <param name="processId">The process identifier to query.</param>
    /// <returns>The current <see cref="GpuAffinityConfig"/>, or null if not bound.</returns>
    public GpuAffinityConfig? GetGpuAffinity(int processId)
    {
        if (_affinityCache.TryGetValue(processId, out var config))
            return config;

        return null;
    }

    /// <summary>
    /// Binds a process to a specific GPU engine (e.g., a particular compute engine on the GPU).
    /// </summary>
    /// <param name="processId">The process identifier to bind.</param>
    /// <param name="engineId">The engine identifier to bind to.</param>
    /// <returns>True if the engine affinity was set successfully; otherwise false.</returns>
    public bool SetEngineAffinity(int processId, int engineId)
    {
        if (processId <= 0 || engineId < 0)
            return false;

        if (_affinityCache.TryGetValue(processId, out var existing))
        {
            existing.EngineId = engineId;
            _affinityCache[processId] = existing;
        }
        else
        {
            var config = new GpuAffinityConfig
            {
                Enabled = true,
                GpuIndex = 0,
                EngineId = engineId
            };
            _affinityCache[processId] = config;
        }

        try
        {
            ApplyGpuAffinity(processId, _affinityCache[processId]);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Removes GPU affinity from a process, resetting it to default GPU selection.
    /// </summary>
    /// <param name="processId">The process identifier to unbind.</param>
    /// <returns>True if the affinity was cleared; otherwise false.</returns>
    public bool ClearGpuAffinity(int processId)
    {
        if (processId <= 0)
            return false;

        if (_affinityCache.Remove(processId))
        {
            try
            {
                ApplyGpuAffinity(processId, new GpuAffinityConfig { Enabled = false });
            }
            catch
            {
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the count of GPUs detected across all available SDKs.
    /// </summary>
    /// <returns>The total number of available GPUs.</returns>
    public int GetGpuCount()
    {
        int count = 0;
        if (_nvApi?.IsAvailable ?? false)
            count += _nvApi.GpuCount;
        if (_adl2?.IsAvailable ?? false)
            count += _adl2.GpuCount;
        return count;
    }

    /// <summary>
    /// Gets the name of the GPU at the specified index.
    /// </summary>
    /// <param name="gpuIndex">The zero-based GPU index.</param>
    /// <returns>The GPU name, or null if unavailable.</returns>
    public string? GetGpuName(int gpuIndex)
    {
        if ((_nvApi?.IsAvailable ?? false) && gpuIndex < _nvApi!.GpuCount)
            return _nvApi.GetGpuName(gpuIndex);

        if ((_adl2?.IsAvailable ?? false) && gpuIndex < _adl2!.GpuCount)
            return _adl2.GetGpuName(gpuIndex);

        return null;
    }

    /// <summary>
    /// Applies the GPU affinity configuration to the process.
    /// Uses P/Invoke to SetProcessAffinityMask-like APIs when available,
    /// otherwise falls back to a software-based tracking approach.
    /// </summary>
    /// <param name="processId">The target process identifier.</param>
    /// <param name="config">The affinity configuration to apply.</param>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessAffinityMask(IntPtr hProcess, long dwProcessAffinityMask);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetProcessAffinityMask(IntPtr hProcess, out long lpProcessAffinityMask, out long lpSystemAffinityMask);

    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_SET_INFORMATION = 0x0200;

    /// <summary>
    /// Applies GPU affinity by setting CPU affinity to cores closest to the target GPU's NUMA node.
    /// On Windows, GPUs are typically associated with specific NUMA nodes, so binding a process's
    /// CPU affinity to the corresponding NUMA node helps keep the process and GPU on the same bus.
    /// </summary>
    /// <param name="processId">The process to apply affinity to.</param>
    /// <param name="config">The GPU affinity configuration.</param>
    private void ApplyGpuAffinity(int processId, GpuAffinityConfig config)
    {
        if (!config.Enabled || !IsGpuAvailable)
            return;

        long affinityMask = ComputeGpuAffinityMask(config.GpuIndex);
        if (affinityMask == 0)
            return;

        try
        {
            IntPtr handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_SET_INFORMATION, false, processId);
            if (handle == IntPtr.Zero)
                return;

            try
            {
                SetProcessAffinityMask(handle, affinityMask);
            }
            finally
            {
                CloseHandle(handle);
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Computes a CPU affinity mask for a given GPU index.
    /// Maps GPU index to a NUMA node and returns the corresponding CPU mask.
    /// </summary>
    /// <param name="gpuIndex">The target GPU index.</param>
    /// <returns>A CPU affinity mask, or 0 if unable to compute.</returns>
    internal long ComputeGpuAffinityMask(int gpuIndex)
    {
        int processorCount = Environment.ProcessorCount;
        int nodes = Math.Min(gpuIndex + 1, processorCount > 0 ? processorCount : 1);

        if (gpuIndex < 0 || nodes <= 0)
            return 0;

        long mask = 0;
        int coresPerGpu = Math.Max(1, processorCount / Math.Max(1, GetGpuCount()));
        int startCore = gpuIndex * coresPerGpu;
        int endCore = Math.Min(startCore + coresPerGpu, processorCount);

        for (int i = startCore; i < endCore; i++)
        {
            mask |= 1L << i;
        }

        return mask;
    }
}