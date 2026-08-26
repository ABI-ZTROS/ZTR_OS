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
        WebView.CoreWebView2.Settings.ZoomFactor = 1.0;

        string html = LoadAndInjectApiUrl();
        WebView.NavigateToString(html);

        StatusText.Text = "Ready";
    }

    private string LoadAndInjectApiUrl()
    {
        string baseUrl = $"http://localhost:{_port}";
        string htmlPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html");

        if (!File.Exists(htmlPath))
        {
            return GenerateFallbackHtml(baseUrl);
        }

        string html = File.ReadAllText(htmlPath);

        string injection = $"<script>window.__API_BASE_URL__='{baseUrl}';</script>";

        if (html.Contains("window.__API_BASE_URL__"))
        {
            html = System.Text.RegularExpressions.Regex.Replace(
                html,
                @"window\.__API_BASE_URL__='[^']*'",
                $"window.__API_BASE_URL__='{baseUrl}'");
        }
        else
        {
            html = html.Replace("</head>", $"{injection}</head>");
        }

        return html;
    }

    private static string GenerateFallbackHtml(string baseUrl)
    {
        return $$"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>ZTR_OS</title>
    <script>window.__API_BASE_URL__='{{baseUrl}}';</script>
    <style>
        body {
            font-family: 'Segoe UI', sans-serif;
            background: #0a0e1a;
            color: #e0e6ed;
            display: flex;
            align-items: center;
            justify-content: center;
            height: 100vh;
            margin: 0;
        }
        .container {
            text-align: center;
            padding: 40px;
        }
        .spinner {
            border: 3px solid #1a1f2e;
            border-top: 3px solid #00ff88;
            border-radius: 50%;
            width: 40px;
            height: 40px;
            animation: spin 1s linear infinite;
            margin: 0 auto 20px;
        }
        @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }
        .status {
            color: #00d4ff;
            font-family: Consolas, monospace;
            font-size: 14px;
        }
        .port-info {
            color: #666;
            font-size: 12px;
            margin-top: 10px;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="spinner"></div>
        <div class="status">Loading ZTR_OS Control Panel...</div>
        <div class="port-info">API: {{baseUrl}}</div>
    </div>
</body>
</html>
""";
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
