namespace ZTR.Models;

public record HardwareStateResponse(
    CpuState Cpu,
    GpuState Gpu,
    BatteryState Battery,
    FanState Fan,
    DateTime Timestamp);

public record SetPerformanceModeRequest(AsusMode Mode);
public record SetGpuModeRequest(AsusGPU Mode);
public record SetFanCurveRequest(AsusFan Device, byte[] Curve);
public record SetPowerLimitRequest(int SPL, int SPPT, int FPPT);
public record SetCpuAffinityRequest(int ProcessId, long AffinityMask);
public record SetGpuAffinityRequest(int ProcessId, int GpuIndex);
public record SetAuraModeRequest(AuraMode Mode, AuraZone Zone, byte R, byte G, byte B);
public record MlpConfigUpdateRequest(MlpConfig Config);
public record BindingPolicyRequest(int ProcessId, BindingStrategy Strategy);
public record SetBindingRequest(List<int> Affinity);

public record ApiResponse<T>(bool Success, T? Data, string? Error = null);
public record ApiResponse(bool Success, string? Error = null);