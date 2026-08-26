using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ZTR.Desktop;

public partial class MainWindow : Window
{
    private readonly ApiServerHost _apiServer = new();

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

        string wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (!Directory.Exists(wwwrootPath))
        {
            StatusText.Text = "ERROR: Frontend files not found";
            MessageBox.Show($"Frontend files not found at:\n{wwwrootPath}\n\nPlease ensure the desktop package is complete.",
                "ZTR_OS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "app.local", wwwrootPath,
            Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

        string apiUrl = $"http://localhost:{_apiServer.Port}";
        string injectScript = $"window.__API_BASE_URL__='{apiUrl}';window.__IS_DESKTOP__=true;";
        WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(injectScript);

        WebView.Source = new Uri("http://app.local/index.html");

        StatusText.Text = "Ready";
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        StatusText.Text = "Shutting down...";
        _apiServer.StopAsync().Wait(TimeSpan.FromSeconds(5));
    }
}
