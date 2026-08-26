using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using ZTR.Api.Extensions;
using ZTR.Api.Hubs;
using ZTR.Api.Middleware;

namespace ZTR.Desktop;

public partial class MainWindow : Window
{
    private readonly ApiServerHost _apiServer = new();
    private int _port;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Starting embedded API server...";

        _port = GetAvailablePort();
        bool serverStarted = await _apiServer.StartAsync(_port);

        if (!serverStarted)
        {
            StatusText.Text = "ERROR: Failed to start API server";
            MessageBox.Show("Failed to start embedded API server.", "ZTR_OS Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        StatusText.Text = "Loading interface...";
        PortText.Text = $"Port: {_port}";

        await WebView.EnsureCoreWebView2Async();
        WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

        string url = $"http://localhost:{_port}/";
        WebView.Source = new Uri(url);

        StatusText.Text = "Ready";
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        StatusText.Text = "Shutting down...";
        Dispatcher.ShutdownStarted += (_, _) => { };
        _apiServer.StopAsync().Wait(TimeSpan.FromSeconds(5));
    }
}
