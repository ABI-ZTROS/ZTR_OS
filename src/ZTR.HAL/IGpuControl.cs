using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Interface for GPU control operations.
/// </summary>
public interface IGpuControl : IDisposable
{
    bool IsNvidia { get; }
    bool IsAmd { get; }
    bool IsValid { get; }
    string FullName { get; }
    int GpuIndex { get; }
    
    int? GetCurrentTemperature();
    int? GetHotspotTemperature();
    int? GetGpuUse();
    (long usedMb, long totalMb)? GetVramInfo();
    float? GetGpuPower();
    
    bool SetClocks(int coreOffset, int memoryOffset);
    bool ResetClocks();
    bool SetPowerLimit(int powerLimit);

    bool SetFanSpeed(int speed);
    int? GetFanSpeed();
    (int coreClockMHz, int memoryClockMHz)? GetClockInfo();
    
    void KillGpuApps();
    
    GpuState GetState();
}