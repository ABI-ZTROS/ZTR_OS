using Microsoft.AspNetCore.SignalR;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Hubs;

public class SensorSignalRBridge : IDisposable
{
    private readonly SensorQueue _queue;
    private readonly IHubContext<HardwareDataHub> _hardwareHubContext;
    private readonly IHubContext<SensorHub> _sensorHubContext;
    private readonly ILogger<SensorSignalRBridge> _logger;
    private bool _disposed;

    public SensorSignalRBridge(
        SensorQueue queue,
        IHubContext<HardwareDataHub> hardwareHubContext,
        IHubContext<SensorHub> sensorHubContext,
        ILogger<SensorSignalRBridge> logger)
    {
        _queue = queue;
        _hardwareHubContext = hardwareHubContext;
        _sensorHubContext = sensorHubContext;
        _logger = logger;

        _queue.StateEnqueued += OnStateEnqueued;
        _logger.LogInformation("SensorSignalRBridge initialized and subscribed to SensorQueue events");
    }

    private async void OnStateEnqueued(object? sender, HardwareState state)
    {
        if (_disposed) return;

        try
        {
            var dto = MapToFrontendDto(state);
            var hardwareTask = _hardwareHubContext.Clients.All.SendCoreAsync(
                "HardwareUpdate",
                new object[] { dto });
            var sensorTask = _sensorHubContext.Clients.All.SendCoreAsync(
                "SensorUpdate",
                new object[] { state });
            await Task.WhenAll(hardwareTask, sensorTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push sensor state via SignalR");
        }
    }

    public void PushState(HardwareState state)
    {
        if (_disposed) return;

        try
        {
            var dto = MapToFrontendDto(state);
            _hardwareHubContext.Clients.All.SendCoreAsync(
                "HardwareUpdate",
                new object[] { dto }).Wait();
            _sensorHubContext.Clients.All.SendCoreAsync(
                "SensorUpdate",
                new object[] { state }).Wait();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push state via SignalR");
        }
    }

    internal static object MapToFrontendDto(HardwareState state)
    {
        var cpu = new
        {
            usage = state.Cpu.Usage,
            temperature = state.Cpu.Temperature,
            powerDraw = state.Cpu.Power,
            coreCount = 0,
            threadCount = 0,
            cores = Array.Empty<object>()
        };

        var gpu = new
        {
            usage = state.Gpu.Usage,
            temperature = state.Gpu.Temperature,
            powerDraw = state.Gpu.Power,
            clockSpeed = state.Gpu.CoreClockMHz,
            memoryUsed = (int)state.Gpu.UsedVramMB,
            memoryTotal = (int)state.Gpu.TotalVramMB,
            fans = state.Fan.GpuFanSpeed
        };

        var battery = new
        {
            percentage = state.Battery.ChargePercent,
            status = state.Battery.IsCharging ? "AC" : "DC",
            timeRemaining = 0,
            powerDraw = state.Battery.ChargeLimit
        };

        var fans = new[]
        {
            new { id = 1, name = "CPU Fan", speed = state.Fan.CpuFanSpeed, targetSpeed = state.Fan.CpuFanRpm, mode = "auto" },
            new { id = 2, name = "GPU Fan", speed = state.Fan.GpuFanSpeed, targetSpeed = state.Fan.GpuFanRpm, mode = "auto" },
            new { id = 3, name = "Mid Fan", speed = state.Fan.MidFanSpeed, targetSpeed = 0, mode = "auto" }
        };

        var memory = new
        {
            used = 0,
            total = 0,
            available = 0
        };

        return new
        {
            cpu,
            gpu,
            battery,
            fans = (object[])fans,
            memory,
            timestamp = state.Timestamp
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _queue.StateEnqueued -= OnStateEnqueued;
        _logger.LogInformation("SensorSignalRBridge disposed");
    }
}