namespace ZTR.Api.DTOs;

public record HardwareResponse(
    CpuResponse Cpu,
    GpuResponse Gpu,
    BatteryResponse Battery,
    List<FanResponse> Fans,
    MemoryResponse Memory,
    DateTime Timestamp
);

public record CpuResponse(
    int Usage,
    int Temperature,
    int PowerDraw,
    int CoreCount,
    int ThreadCount,
    List<CoreResponse> Cores
);

public record CoreResponse(int Id, int Usage, int Temperature);

public record GpuResponse(
    int Usage,
    int Temperature,
    int PowerDraw,
    int ClockSpeed,
    long MemoryUsed,
    long MemoryTotal,
    int Fans
);

public record BatteryResponse(
    int Percentage,
    string Status,
    int TimeRemaining,
    int PowerDraw
);

public record FanResponse(int Id, string Name, int Speed, int TargetSpeed, string Mode);

public record MemoryResponse(long Used, long Total, long Available);
