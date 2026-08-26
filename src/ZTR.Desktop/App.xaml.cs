using System.IO;
using System.Windows;
using System.Windows.Threading;
using ZTR.Desktop.Features.UserAgreement.Services;
using ZTR.Desktop.Features.UserAgreement.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ZTR.Desktop;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public static IServiceProvider Services
    {
        get
        {
            var app = Current as App;
            return app?._serviceProvider ?? throw new InvalidOperationException("DI 容器尚未初始化");
        }
    }

    internal void SetServiceProvider(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    static App()
    {
        ForceLog.Write("[BOOT-0] App 静态构造函数执行");
        ForceLog.Write($"[BOOT-0] BaseDir = {AppContext.BaseDirectory}");
        ForceLog.Write($"[BOOT-0] OS = {Environment.OSVersion}");
        ForceLog.Write($"[BOOT-0] x64 = {Environment.Is64BitProcess} CPU = {Environment.ProcessorCount}");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            ForceLog.Write($"[FATAL] AppDomain.UnhandledException IsTerminating={e.IsTerminating}");
            ForceLog.Write(e.ExceptionObject?.ToString() ?? "(ExceptionObject 不是 Exception)");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            ForceLog.Write($"[WARN] TaskScheduler.UnobservedTaskException: {e.Exception}");
            e.SetObserved();
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        ForceLog.Write("[BOOT-1] OnStartup 入口");

        var safeCulture = CultureSafety.ApplySafeCulture();
        CultureSafety.HookDispatcher(Dispatcher.CurrentDispatcher, safeCulture);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DispatcherUnhandledException += (_, e2) =>
        {
            ForceLog.Write($"[FATAL] DispatcherUnhandledException: {e2.Exception}");
            GlobalExceptionHandler.ShowCrashReport(e2.Exception);
            e2.Handled = true;
        };

        LoggingSetup.Configure();

        try
        {
            base.OnStartup(e);
            ForceLog.Write("[BOOT] base.OnStartup 成功");

            var agreementService = new UserAgreementService();
            agreementService.Load();
            if (agreementService.RequiresReagreement)
            {
                ForceLog.Write("[BOOT] 需要用户同意协议，显示协议窗口...");
                var agreementWindow = new UserAgreementWindow(agreementService);
                var agreed = agreementWindow.ShowDialog() == true;
                if (!agreed)
                {
                    ForceLog.Write("[BOOT] 用户未同意协议，终止启动");
                    Shutdown();
                    return;
                }
                ForceLog.Write($"[BOOT] 用户已同意协议 v{agreementService.CurrentAgreementVersion}");
            }

            var startupWindow = new StartupWindow();
            startupWindow.Show();
            MainWindow = startupWindow;

            var boot = new BootSequence(startupWindow);
            _ = Task.Run(async () =>
            {
                try
                {
                    await boot.RunAsync(agreementService);

                    Dispatcher.Invoke(() =>
                    {
                        var mainWindow = new MainWindow();
                        mainWindow.Show();
                        startupWindow.Close();
                        MainWindow = mainWindow;
                        ShutdownMode = ShutdownMode.OnMainWindowClose;
                    });
                }
                catch (Exception ex)
                {
                    ForceLog.Write($"[FATAL] Boot 序列异常: {ex}");
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"启动失败：{ex.Message}", "ZTR_OS 启动错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Shutdown();
                    });
                }
            });
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[FATAL] OnStartup 异常: {ex}");
            MessageBox.Show($"启动失败：{ex.Message}\n\n{ex.StackTrace}", "ZTR_OS 启动错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }
}
