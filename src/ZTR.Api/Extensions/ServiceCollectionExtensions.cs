using Microsoft.Extensions.DependencyInjection;
using ZTR.Api.Hubs;
using ZTR.HAL;
using ZTR.Intelligence;
using ZTR.Models;

namespace ZTR.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddZTRServices(this IServiceCollection services)
    {
        services.AddSingleton<AsusAcpi>();
        services.AddSingleton<AsusHid>();
        services.AddSingleton<WmiHelper>();
        services.AddSingleton<BatteryControl>();
        services.AddSingleton<PowerLimitManager>();
        services.AddSingleton<GPUModeControl>();
        services.AddSingleton<ModeControl>();
        services.AddSingleton<AuraLighting>();
        services.AddSingleton<CpuAffinityManager>();
        services.AddSingleton<GpuAffinityManager>();
        services.AddSingleton<TopologyService>();
        services.AddSingleton<ProcessTracker>();
        services.AddSingleton<SensorQueue>();
        services.AddSingleton<ISystemSensorFallback>(sp =>
        {
            var logger = sp.GetService<ILogger<SystemSensorFallback>>();
            return new SystemSensorFallback(logger);
        });

        services.AddSingleton<GpuSensorService>(sp =>
        {
            var logger = sp.GetService<ILogger<GpuSensorService>>();
            var service = new GpuSensorService(logger);
            service.Initialize();
            return service;
        });

        services.AddSingleton<IGpuControl>(sp =>
        {
            var gpuService = sp.GetRequiredService<GpuSensorService>();
            var primary = gpuService.GpuControls.FirstOrDefault();
            return primary ?? new EmptyGpuControl();
        });

        services.AddSingleton<SensorPipeline>(sp =>
        {
            var acpi = sp.GetService<AsusAcpi>();
            var gpuControl = sp.GetService<IGpuControl>();
            var batteryControl = sp.GetService<BatteryControl>();
            var systemFallback = sp.GetService<ISystemSensorFallback>();
            var logger = sp.GetService<ILogger<SensorPipeline>>();
            return new SensorPipeline(acpi, gpuControl, batteryControl, null, null, null, systemFallback, logger);
        });
        services.AddSingleton<DeviceProbe>();
        services.AddSingleton<BindingPolicy>();

        services.AddSingleton<MlpConfig>(sp => new MlpConfig());
        services.AddSingleton<MlpNetwork>();
        services.AddSingleton<SensorFeatureExtractor>();
        services.AddSingleton<PerformanceDecisionEngine>();
        services.AddSingleton<DecisionLogger>();
        services.AddSingleton<OnlineLearner>();
        services.AddSingleton<PredictiveScheduler>();

        services.AddSingleton<SensorSignalRBridge>();
        services.AddHostedService<SensorBackgroundService>();

        services.AddSingleton<ScreenControl>();
        services.AddSingleton<KeyboardControl>();
        services.AddSingleton<GpuTuningService>();
        services.AddSingleton<AutomationService>();
        services.AddSingleton<AnimeMatrixEngine>();
        services.AddSingleton<XgmMobileControl>();
        services.AddSingleton<BiosUpdateChecker>();

        return services;
    }
}

internal class EmptyGpuControl : IGpuControl
{
    public bool IsNvidia => false;
    public bool IsAmd => false;
    public bool IsValid => false;
    public string FullName => "No GPU detected";
    public int GpuIndex => 0;

    public int? GetCurrentTemperature() => null;
    public int? GetHotspotTemperature() => null;
    public int? GetGpuUse() => null;
    public (long usedMb, long totalMb)? GetVramInfo() => null;
    public float? GetGpuPower() => null;
    public bool SetClocks(int coreOffset, int memoryOffset) => false;
    public bool ResetClocks() => false;
    public bool SetPowerLimit(int powerLimit) => false;
    public bool SetFanSpeed(int speed) => false;
    public int? GetFanSpeed() => null;
    public (int coreClockMHz, int memoryClockMHz)? GetClockInfo() => null;
    public void KillGpuApps() { }
    public GpuState GetState() => new GpuState();
    public void Dispose() { }
}