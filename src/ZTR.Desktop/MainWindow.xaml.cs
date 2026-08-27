using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ZTR.Desktop.Features.WebView2.Services;

namespace ZTR.Desktop;

public partial class MainWindow : Window
{
    private readonly ApiServerHost _apiServer = new();
    private IWebView2BridgeService? _bridge;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Starting embedded API server...";

        bool serverStarted = await _apiServer.StartAsync();

        if (!serverStarted)
        {
            StatusText.Text = "ERROR: No port available in range 5000-5010";
            MessageBox.Show("Failed to allocate API server port (5000-5010).\nPlease close the conflicting application and try again.",
                "ZTR_OS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        StatusText.Text = "Initializing WebView2...";
        PortText.Text = $"API: localhost:{_apiServer.Port}";

        await WebView.EnsureCoreWebView2Async();
        WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

        // V4 FIXED: Attach WebView2 bridge after CoreWebView2 is initialized.
        // Previously the bridge was registered in DI but never resolved or
        // attached → JS↔C# bidirectional communication was 0% connected.
        try
        {
            if (App.Services.GetService<IWebView2BridgeService>() is { } bridge)
            {
                _bridge = bridge;
                bridge.Attach(WebView.CoreWebView2);
                // Register core handlers for JS→C# bridge calls
                bridge.RegisterHandler("getApiConfig", async _ =>
                {
                    var url = $"http://localhost:{_apiServer.Port}";
                    return new { apiBaseUrl = url, isDesktop = true };
                });
                bridge.RegisterHandler("navigate", async args =>
                {
                    if (args.Length > 0 && args[0] is string path)
                    {
                        WebView.CoreWebView2.Navigate(path);
                    }
                    return null;
                });
                ForceLog.Write("[BRIDGE] WebView2Bridge attached - JS↔C# communication enabled");
            }
        }
        catch (System.Exception ex)
        {
            ForceLog.Write($"[BRIDGE] Failed to attach WebView2Bridge: {ex.Message}");
        }

        await WebView.CoreWebView2.ExecuteScriptAsync(
            "localStorage.clear(); sessionStorage.clear();");

        string wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");

        if (!Directory.Exists(wwwrootPath) || !File.Exists(Path.Combine(wwwrootPath, "index.html")))
        {
            TryPopulateWwwroot(wwwrootPath);
        }

        if (!Directory.Exists(wwwrootPath) || !File.Exists(Path.Combine(wwwrootPath, "index.html")))
        {
            GenerateFallbackIndex(wwwrootPath);
        }

        if (!File.Exists(Path.Combine(wwwrootPath, "index.html")))
        {
            StatusText.Text = "ERROR: Frontend files not found";
            MessageBox.Show(
                $"Frontend files not found at:\n{wwwrootPath}\n\n" +
                "This usually means the frontend was not built during compilation.\n\n" +
                "Solutions:\n" +
                "1. Run 'npm ci && npm run build' in the frontend folder, then rebuild\n" +
                "2. Or manually copy the frontend/dist contents to the wwwroot folder\n\n" +
                "The API backend is running correctly. Visit http://localhost:{_apiServer.Port}/swagger for API documentation.",
                "ZTR_OS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "app.local", wwwrootPath,
            Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

        string apiUrl = $"http://localhost:{_apiServer.Port}";
        string injectScript = $"window.__API_BASE_URL__='{apiUrl}';window.__IS_DESKTOP__=true;";
        await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(injectScript);

        WebView.Source = new Uri("http://app.local/index.html");

        StatusText.Text = "Ready";
    }

    private void TryPopulateWwwroot(string wwwrootPath)
    {
        string baseDir = AppContext.BaseDirectory;
        string[] candidateDirs =
        {
            Path.Combine(baseDir, "..", "..", "..", "..", "frontend", "dist"),
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "frontend", "dist"),
            Path.Combine(baseDir, "..", "frontend", "dist"),
            Path.Combine(baseDir, "frontend", "dist"),
        };

        foreach (var candidate in candidateDirs)
        {
            string fullPath = Path.GetFullPath(candidate);
            if (Directory.Exists(fullPath) && File.Exists(Path.Combine(fullPath, "index.html")))
            {
                try
                {
                    Directory.CreateDirectory(wwwrootPath);
                    foreach (var file in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories))
                    {
                        string relativePath = file.Substring(fullPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string destPath = Path.Combine(wwwrootPath, relativePath);
                        string? destDir = Path.GetDirectoryName(destPath);
                        if (destDir != null)
                            Directory.CreateDirectory(destDir);
                        File.Copy(file, destPath, overwrite: true);
                    }
                    StatusText.Text = "Frontend files copied from source";
                    return;
                }
                catch
                {
                }
            }
        }
    }

    private void GenerateFallbackIndex(string wwwrootPath)
    {
        Directory.CreateDirectory(wwwrootPath);

        string fallbackHtml = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>ZTR_OS - Frontend Not Built</title>
    <style>
        body { font-family: 'Segoe UI', sans-serif; background: #0a0e1a; color: #e0e6ed; display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; }
        .card { background: #1a1f2e; padding: 40px; border-radius: 12px; max-width: 600px; border: 1px solid #2a3040; }
        h1 { color: #ff4757; margin-top: 0; }
        h2 { color: #00d4ff; }
        code { background: #0a0e1a; padding: 2px 6px; border-radius: 4px; color: #00ff88; }
        .api-link { color: #00d4ff; }
        .steps { line-height: 1.8; }
    </style>
</head>
<body>
    <div class="card">
        <h1>&#9888; Frontend Not Built</h1>
        <h2>API Backend is Running</h2>
        <p>The ZTR_OS backend API is active but the frontend UI files were not found.</p>
        <div class="steps">
            <p><strong>To fix this:</strong></p>
            <ol>
                <li>Open a terminal in the project root</li>
                <li>Run: <code>cd frontend &amp;&amp; npm ci &amp;&amp; npm run build</code></li>
                <li>Rebuild the ZTR.Desktop project</li>
            </ol>
        </div>
        <p>Backend API is available at: <a class="api-link" href="/swagger" onclick="window.open('http://localhost:PORT/swagger');return false;">http://localhost:PORT/swagger</a></p>
        <p style="color: #888; font-size: 12px;">Replace PORT with the port shown in the status bar below.</p>
    </div>
    <script>
        // Replace PORT placeholder
        document.querySelectorAll('.api-link, .card p').forEach(el => {
            el.innerHTML = el.innerHTML.replace('PORT', window.location.port || '5000');
        });
    </script>
</body>
</html>
""";

        File.WriteAllText(Path.Combine(wwwrootPath, "index.html"), fallbackHtml);
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        StatusText.Text = "Shutting down...";

        // V4 FIXED: Detach bridge before stopping API server
        try
        {
            _bridge?.Detach();
        }
        catch { }

        _apiServer.StopAsync().Wait(TimeSpan.FromSeconds(5));
    }
}
