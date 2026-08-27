using System.Diagnostics.CodeAnalysis;
using System.Management;
using ZTR.Api.DTOs;
using ZTR.Models;

namespace ZTR.Api.Mappers;

public static class HardwareMapper
{
    public static HardwareResponse ToFrontend(HardwareState state)
    {
        // V10 FIXED: Align Cores list cap with CoreCount so frontend doesn't
        // render out-of-bounds. Both use Math.Min(coreCount, 16) consistently.
        int coreCount = Environment.ProcessorCount;
        int threadCount = GetLogicalProcessors();
        int coreCap = Math.Min(coreCount, 16);

        var cores = GenerateCoreStates(coreCap, threadCount, state.Cpu.Usage, state.Cpu.Temperature);

        return new HardwareResponse(
            Cpu: new CpuResponse(
                Usage: state.Cpu.Usage,
                Temperature: state.Cpu.Temperature,
                PowerDraw: state.Cpu.Power,
                CoreCount: coreCap,
                ThreadCount: threadCount,
                Cores: cores
            ),
            Gpu: new GpuResponse(
                Usage: state.Gpu.Usage,
                Temperature: state.Gpu.Temperature,
                PowerDraw: state.Gpu.Power,
                ClockSpeed: state.Gpu.CoreClockMHz,
                MemoryUsed: state.Gpu.UsedVramMB,
                MemoryTotal: state.Gpu.TotalVramMB,
                Fans: state.Fan.GpuFanSpeed > 0 ? 1 : 0
            ),
            Battery: new BatteryResponse(
                Percentage: state.Battery.ChargePercent,
                // V7 FIXED: Use shared battery status mapping.
                // Previously this returned "Charging"/"AC" while SignalR returned "AC"/"DC"
                // (opposite semantics). Now both use "AC" (charging) / "DC" (not charging).
                Status: MapBatteryStatus(state.Battery.IsCharging),
                TimeRemaining: 0,
                PowerDraw: 0
            ),
            Fans: BuildFanList(state.Fan),
            Memory: new MemoryResponse(0, 0, 0),
            Timestamp: state.Timestamp
        );
    }

    private static List<CoreResponse> GenerateCoreStates(int coreCount, int threadCount, int totalUsage, int temperature)
    {
        var cores = new List<CoreResponse>();

        for (int i = 0; i < Math.Min(coreCount, 16); i++)
        {
            int variance = i % 2 == 0 ? 2 : -2;
            cores.Add(new CoreResponse(
                Id: i,
                Usage: Math.Clamp(totalUsage + variance, 0, 100),
                Temperature: Math.Clamp(temperature + variance, 0, 110)
            ));
        }

        return cores;
    }

    // V7 FIXED: Shared battery status mapping used by both REST and SignalR paths
    public static string MapBatteryStatus(bool isCharging)
    {
        return isCharging ? "AC" : "DC";
    }

    private static List<FanResponse> BuildFanList(FanState fan)
    {
        var fans = new List<FanResponse>
        {
            new(1, "CPU Fan", fan.CpuFanSpeed, fan.CpuFanSpeed, "automatic"),
            new(2, "GPU Fan", fan.GpuFanSpeed, fan.GpuFanSpeed, "automatic")
        };

        if (fan.MidFanSpeed > 0)
        {
            fans.Add(new FanResponse(3, "Mid Fan", fan.MidFanSpeed, fan.MidFanSpeed, "automatic"));
        }

        return fans;
    }

    [SuppressMessage("Interoperability", "CA1416")]
    private static int GetLogicalProcessors()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT NumberOfLogicalProcessors FROM Win32_ComputerSystem");
            using var results = searcher.Get();

            foreach (System.Management.ManagementObject obj in results)
            {
                return Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
            }
        }
        catch { }

        return Environment.ProcessorCount;
    }
}
