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
        services.AddSingleton<SensorPipeline>();
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

        return services;
    }
}