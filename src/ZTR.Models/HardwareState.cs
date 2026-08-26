namespace ZTR.Models;

public class HardwareState
{
    public CpuState Cpu { get; set; } = new();
    public GpuState Gpu { get; set; } = new();
    public BatteryState Battery { get; set; } = new();
    public FanState Fan { get; set; } = new();
    public IReadOnlyList<SensorReading> Sensors { get; set; } = Array.Empty<SensorReading>();
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class CpuState
{
    public int Temperature { get; set; }
    public int Usage { get; set; }
    public int Power { get; set; }
    public int ClockMHz { get; set; }
    public int PowerLimit { get; set; }
}

public class GpuState
{
    public int Temperature { get; set; }
    public int HotspotTemperature { get; set; }
    public int Usage { get; set; }
    public int Power { get; set; }
    public long UsedVramMB { get; set; }
    public long TotalVramMB { get; set; }
    public int CoreClockMHz { get; set; }
    public int MemoryClockMHz { get; set; }
}

public class BatteryState
{
    public int ChargePercent { get; set; }
    public bool IsCharging { get; set; }
    public int ChargeLimit { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class FanState
{
    public int CpuFanSpeed { get; set; }
    public int CpuFanRpm { get; set; }
    public int GpuFanSpeed { get; set; }
    public int GpuFanRpm { get; set; }
    public int MidFanSpeed { get; set; }
}

public class SensorReading
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public SensorType Type { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum SensorType
{
    Temperature,
    Power,
    Usage,
    Clock,
    Fan,
    Voltage,
    Current
}