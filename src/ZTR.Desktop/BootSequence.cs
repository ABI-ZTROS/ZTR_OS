using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ZTR.Desktop.Features.Config.Services;
using ZTR.Desktop.Features.Process.Services;
using ZTR.Desktop.Features.Theme.Services;
using ZTR.Desktop.Features.UserAgreement.Services;
using ZTR.Desktop.Features.WebView2.Services;
using ZTR.Desktop.Features.WindowEffects.Services;

namespace ZTR.Desktop;

public class BootSequence
{
    private readonly StartupWindow _startupWindow;
    private readonly BootStats _stats = new();
    private IServiceProvider? _serviceProvider;

    public BootSequence(StartupWindow startupWindow)
    {
        _startupWindow = startupWindow;
    }

    public IServiceProvider ServiceProvider => _serviceProvider
        ?? throw new InvalidOperationException("Boot 尚未完成");

    public async Task RunAsync(IUserAgreementService agreementService)
    {
        await Step(5, "正在搭建 DI 容器...", "[BOOT] ZTR_OS 启动序列开始");

        var services = new ServiceCollection();
        services.AddSingleton<Serilog.ILogger>(Log.Logger);

        await RegisterServices(services, agreementService);

        await Step(95, "正在构建服务容器...", "[BUILD] 验证服务契约拓扑...");
        _serviceProvider = services.BuildServiceProvider();

        if (Application.Current is App app)
        {
            app.SetServiceProvider((ServiceProvider)_serviceProvider);
        }

        await Step(100, "启动完成", $"[OK] ServiceProvider 已构建 ({_stats.Ok} OK / {_stats.Fail} FAIL)");

        ForceLog.Write($"[BOOT] Boot 完成: {_stats.Ok} OK, {_stats.Fail} FAIL");
    }

    private async Task RegisterServices(IServiceCollection services, IUserAgreementService agreementService)
    {
        // V8 FIXED: Use the already-agreed agreementService instance instead of creating
        // a new un-agreed one. Previously the pre-agreed instance was thrown away and a fresh
        // UserAgreementService was registered in DI → IUserAgreementService would get an
        // instance that bypassed the protocol gate.
        await Register<IUserAgreementService>(agreementService, services, 10, "用户协议服务", "状态持久化");
        await Register<IThemeService, ThemeService>(services, 20, "主题服务", "亮/暗切换");
        await Register<IConfigurationService, ConfigurationService>(services, 30, "配置服务", "JSON 持久化");
        await Register<IWindowEffectsService, WindowEffectsService>(services, 45, "窗口特效服务", "Mica/圆角/暗色标题栏");
        await Register<IWebView2BridgeService, WebView2BridgeService>(services, 60, "WebView2 桥接服务", "JS↔C# 双向通信");
        await Register<IProcessManagerService, ProcessManagerService>(services, 75, "进程管理服务", "亲和性/优先级");
    }

    // V8 FIXED: New overload that registers a pre-built instance instead of creating one
    private async Task Register<TService>(TService instance, IServiceCollection services, int percent, string displayName, string description)
        where TService : class
    {
        _startupWindow.SetProgress(percent, $"加载 {displayName}...");
        _startupWindow.AppendLog($"[LOAD] {displayName} ({description})...");
        await Task.Delay(40);
        try
        {
            services.AddSingleton<TService>(instance);
            _stats.Ok++;
            _startupWindow.AppendLog($"[OK]   {displayName,-20} ← {instance.GetType().Name} // {description}", isSuccess: true);
        }
        catch (Exception ex)
        {
            _stats.Fail++;
            _startupWindow.AppendLog($"[ERR]  {displayName,-20} 加载失败: {ex.Message}", isError: true);
        }
        await Task.Delay(40);
    }

    private async Task Register<TService, TImpl>(IServiceCollection services, int percent, string displayName, string description)
        where TService : class
        where TImpl : class, TService
    {
        _startupWindow.SetProgress(percent, $"加载 {displayName}...");
        _startupWindow.AppendLog($"[LOAD] {displayName} ({description})...");
        await Task.Delay(40);
        try
        {
            services.AddSingleton<TService, TImpl>();
            _stats.Ok++;
            _startupWindow.AppendLog($"[OK]   {displayName,-20} ← {typeof(TImpl).Name} // {description}", isSuccess: true);
        }
        catch (Exception ex)
        {
            _stats.Fail++;
            _startupWindow.AppendLog($"[ERR]  {displayName,-20} 加载失败: {ex.Message}", isError: true);
        }
        await Task.Delay(40);
    }

    private async Task Step(int percent, string status, string log)
    {
        _startupWindow.SetProgress(percent, status);
        _startupWindow.AppendLog(log);
        await Task.Delay(100);
    }
}
