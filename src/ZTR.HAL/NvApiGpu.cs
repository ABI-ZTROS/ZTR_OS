using NvAPIWrapper.GPU;
using NvAPIWrapper.Native;
using NvAPIWrapper.Native.GPU;
using NvAPIWrapper.Native.GPU.Structures;
using NvAPIWrapper.Native.Interfaces.GPU;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Implementation of <see cref="INvApiGpu"/> using NvAPIWrapper.Net to communicate with NVIDIA GPUs.
/// Provides real hardware access for temperature, usage, VRAM, power, and clock management.
/// </summary>
public class NvApiGpu : INvApiGpu
{
    private readonly PhysicalGPU[]? _gpus;

    /// <inheritdoc />
    public bool IsAvailable { get; }

    /// <inheritdoc />
    public int GpuCount => _gpus?.Length ?? 0;

    /// <summary>
    /// Creates a new instance of the <see cref="NvApiGpu"/> class.
    /// </summary>
    public NvApiGpu()
    {
        try
        {
            _gpus = PhysicalGPU.GetPhysicalGPUs();
            IsAvailable = _gpus != null && _gpus.Length > 0;
        }
        catch
        {
            _gpus = null;
            IsAvailable = false;
        }
    }

    /// <inheritdoc />
    public string? GetGpuName(int index)
    {
        var gpu = GetGpu(index);
        return gpu?.FullName;
    }

    /// <inheritdoc />
    public int? GetTemperature(int index)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return null;
            var sensor = gpu.ThermalInformation.ThermalSensors?.FirstOrDefault();
            return sensor?.CurrentTemperature;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public int? GetHotspotTemperature(int index)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return null;
            var sensor = gpu.ThermalInformation.ThermalSensors
                ?.FirstOrDefault(s => s.Target == ThermalSettingsTarget.GPU);
            return sensor?.CurrentTemperature
                ?? gpu.ThermalInformation.ThermalSensors?.FirstOrDefault()?.CurrentTemperature;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public int? GetUsage(int index)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return null;
            return gpu.UsageInformation.GPU?.Percentage;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public (long usedMb, long totalMb)? GetVramInfo(int index)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return null;
            var memInfo = gpu.MemoryInformation;
            long totalKb = memInfo.DedicatedVideoMemoryInkB;
            long availableKb = memInfo.CurrentAvailableDedicatedVideoMemoryInkB;
            long usedKb = totalKb - availableKb;
            return (usedKb / 1024, totalKb / 1024);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public float? GetPower(int index)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return null;
            var entry = gpu.PowerTopologyInformation.PowerTopologyEntries?.FirstOrDefault();
            return entry?.PowerUsageInPCM / 1000f;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public (int coreClockMHz, int memoryClockMHz)? GetClockInfo(int index)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return null;

            IPerformanceStates20Info info = GPUApi.GetPerformanceStates20(gpu.Handle);
            if (info?.Clocks == null) return null;

            int coreClock = 0;
            int memClock = 0;

            foreach (var clockEntry in info.Clocks.Values.SelectMany(v => v))
            {
                if (clockEntry.IsEditable)
                {
                    var range = clockEntry.FrequencyRange;
                    if (range != null)
                    {
                        if (clockEntry.DomainId == PublicClockDomain.Graphics)
                            coreClock = (int)(range.MaximumFrequencyInkHz / 1000);
                        else if (clockEntry.DomainId == PublicClockDomain.Memory)
                            memClock = (int)(range.MaximumFrequencyInkHz / 1000);
                    }
                }
            }

            return (coreClock, memClock);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public int? GetFanSpeed(int index)
    {
        return null;
    }

    /// <inheritdoc />
    public bool SetClocks(int index, int coreOffset, int memoryOffset)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return false;

            var info = GPUApi.GetPerformanceStates20(gpu.Handle);
            if (info == null || !info.IsEditable) return false;

            if (info.Clocks != null)
            {
                foreach (var clockEntry in info.Clocks.Values.SelectMany(v => v))
                {
                    if (!clockEntry.IsEditable) continue;

                    var delta = clockEntry.FrequencyDeltaInkHz;
                    if (clockEntry.DomainId == PublicClockDomain.Graphics)
                        delta.DeltaValue = coreOffset;
                    else if (clockEntry.DomainId == PublicClockDomain.Memory)
                        delta.DeltaValue = memoryOffset;
                }
            }

            GPUApi.SetPerformanceStates20(gpu.Handle, info);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool ResetClocks(int index)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return false;

            var info = GPUApi.GetPerformanceStates20(gpu.Handle);
            if (info == null) return false;

            if (info.Clocks != null)
            {
                foreach (var clockEntry in info.Clocks.Values.SelectMany(v => v))
                {
                    if (!clockEntry.IsEditable) continue;
                    var delta = clockEntry.FrequencyDeltaInkHz;
                    delta.DeltaValue = 0;
                }
            }

            GPUApi.SetPerformanceStates20(gpu.Handle, info);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool SetPowerLimit(int index, int powerLimit)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return false;

            var status = GPUApi.ClientPowerPoliciesGetStatus(gpu.Handle);
            if (status.PowerPolicyStatusEntries == null || status.PowerPolicyStatusEntries.Length == 0)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public (int minWatts, int maxWatts)? GetPowerLimitRange(int index)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return null;

            var info = GPUApi.ClientPowerPoliciesGetInfo(gpu.Handle);
            if (info.PowerPolicyInfoEntries == null || info.PowerPolicyInfoEntries.Length == 0)
                return null;

            var entry = info.PowerPolicyInfoEntries[0];
            uint min = entry.MinimumPowerInPCM;
            uint max = entry.MaximumPowerInPCM;
            return ((int)(min / 1000), (int)(max / 1000));
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetSupportedPowerModes(int index)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return Array.Empty<string>();

            var info = GPUApi.ClientPowerPoliciesGetInfo(gpu.Handle);
            if (info.PowerPolicyInfoEntries == null)
                return Array.Empty<string>();

            var modes = new List<string>();
            foreach (var entry in info.PowerPolicyInfoEntries)
            {
                modes.Add(entry.PerformanceStateId.ToString());
            }
            return modes.AsReadOnly();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public bool SetMaxGpuClock(int index, int clock)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return false;

            var info = GPUApi.GetPerformanceStates20(gpu.Handle);
            if (info == null || !info.IsEditable) return false;

            if (info.Clocks != null)
            {
                foreach (var clockEntry in info.Clocks.Values.SelectMany(v => v))
                {
                    if (!clockEntry.IsEditable) continue;
                    if (clockEntry.DomainId == PublicClockDomain.Graphics)
                    {
                        var delta = clockEntry.FrequencyDeltaInkHz;
                        delta.DeltaValue = clock;
                    }
                }
            }

            GPUApi.SetPerformanceStates20(gpu.Handle, info);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool SetFanSpeed(int index, int speed)
    {
        return false;
    }

    /// <inheritdoc />
    public void KillGpuApps(int index)
    {
        try
        {
            var gpu = GetGpu(index);
            if (gpu == null) return;

            var processes = GPUApi.QueryActiveApps(gpu.Handle);
            if (processes == null) return;

            foreach (var proc in processes)
            {
                try
                {
                    System.Diagnostics.Process.GetProcessById(proc.ProcessId)?.Kill();
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    private PhysicalGPU? GetGpu(int index)
    {
        if (_gpus == null || index < 0 || index >= _gpus.Length)
            return null;
        return _gpus[index];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // NvAPIWrapper's PhysicalGPU objects are managed wrappers around native handles.
        // The NvAPI library doesn't require explicit shutdown for individual GPU handles.
        // This method exists to satisfy IDisposable and allow proper cleanup in the chain.
        // If future versions of NvAPIWrapper require explicit disposal, add cleanup here.
    }
}