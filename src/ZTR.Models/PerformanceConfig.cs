namespace ZTR.Models;

public class PerformanceConfig
{
    public AsusMode Mode { get; set; }
    public int CpuPowerLimit { get; set; }
    public int GpuPowerLimit { get; set; }
    public int CPUTempLimit { get; set; }
    public int FanCpuMin { get; set; }
    public int FanGpuMin { get; set; }
    public byte[] CpuFanCurve { get; set; } = Array.Empty<byte>();
    public byte[] GpuFanCurve { get; set; } = Array.Empty<byte>();
    public bool AutoApplyFans { get; set; }
    public bool AutoApplyPower { get; set; }
}

public class FanCurvePoint
{
    public int Temperature { get; set; }
    public int Speed { get; set; }
}

public class GpuPerformanceConfig
{
    public AsusGPU Mode { get; set; }
    public int TgpLimit { get; set; }
    public int BoostLimit { get; set; }
    public int TemperatureLimit { get; set; }
    public int CoreClockOffset { get; set; }
    public int MemoryClockOffset { get; set; }
    public int VoltageOffset { get; set; }
}