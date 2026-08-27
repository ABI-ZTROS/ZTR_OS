using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ZTR.Api.Controllers;
using ZTR.Api.Extensions;
using ZTR.Api.Hubs;
using ZTR.Api.Middleware;
using ZTR.HAL;

namespace ZTR.Desktop;

public class ApiServerHost : IDisposable
{
    private WebApplication? _app;
    private bool _disposed;

    public int Port { get; private set; }

    public async Task<bool> StartAsync()
    {
        ForceLog.Write("[API] Embedded API server starting...");

        foreach (int port in Enumerable.Range(5000, 11))
        {
            if (!IsPortAvailable(port)) continue;

            try
            {
                ForceLog.Write($"[API] Trying port {port}...");
                var result = await TryStartOnPort(port);
                if (result)
                {
                    ForceLog.Write($"[API] Successfully started on port {port}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                ForceLog.Write($"[API] Port {port} failed: {ex.Message}");
                continue;
            }
        }

        ForceLog.Write("[API] No available port in range 5000-5010");
        return false;
    }

    private async Task<bool> TryStartOnPort(int port)
    {
        Port = port;

        ForceLog.Write($"[API] Creating WebApplication builder for port {port}...");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>()
        });

        builder.WebHost.UseUrls($"http://localhost:{port}");
        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, port);
        });

        var apiAssembly = typeof(HardwareController).Assembly;
        ForceLog.Write($"[API] Using API assembly: {apiAssembly.FullName}");

        builder.Services.AddControllers()
            .AddApplicationPart(apiAssembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
            {
                Title = "ZTR_OS Desktop API",
                Version = "v1",
                Description = "ZTR_OS Embedded Backend API"
            });

            var xmlFile = $"{apiAssembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Desktop", policy =>
            {
                // file:// is not a valid CORS origin. WebView2 in desktop mode
                // sends requests without an Origin header (null), so we allow null origins.
                policy.SetIsOriginAllowed(origin => origin == null)
                      .WithOrigins("http://app.local")
                      .WithOrigins(Enumerable.Range(5000, 11).Select(p => $"http://localhost:{p}").ToArray())
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        builder.Services.AddSignalR();
        builder.Services.AddHealthChecks();
        builder.Services.AddZTRServices();

        ForceLog.Write("[API] Building WebApplication (resolving DI services)...");

        try
        {
            _app = builder.Build();
            ForceLog.Write("[API] WebApplication built successfully");
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[API] Build FAILED: {ex.Message}");
            ForceLog.Write($"[API] Stack: {ex.StackTrace}");
            if (ex.InnerException != null)
                ForceLog.Write($"[API] Inner: {ex.InnerException.Message}");
            return false;
        }

        ForceLog.Write("[API] Configuring middleware pipeline...");

        _app.UseSwagger();
        _app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "ZTR_OS API v1");
        });

        _app.UseZTRMiddleware();
        _app.UseCors("Desktop");

        _app.MapZTREndpoints();

        // V9 FIXED: Resolve SensorSignalRBridge after building to activate SignalR push chain.
        // Previously registered in DI but never resolved → StateEnqueued event had no
        // subscribers → sensor data collected but never pushed to frontend.
        try
        {
            var bridge = _app.Services.GetRequiredService<SensorSignalRBridge>();
            ForceLog.Write("[API] SensorSignalRBridge resolved - SignalR push chain active");
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[API] WARNING: SensorSignalRBridge resolve failed: {ex.Message}");
        }

        ForceLog.Write("[API] Starting Kestrel server...");

        try
        {
            await _app.StartAsync();
            ForceLog.Write($"[API] Kestrel listening on http://localhost:{port}");
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[API] StartAsync FAILED: {ex.Message}");
            ForceLog.Write($"[API] Stack: {ex.StackTrace}");
            if (_app is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _app = null;
            return false;
        }

        ForceLog.Write("[API] Embedded API server is fully operational");
        return true;
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task StopAsync()
    {
        if (_app != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await _app.StopAsync(cts.Token);
            }
            catch { }
            try
            {
                ((IDisposable)_app).Dispose();
            }
            catch { }
            _app = null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopAsync().Wait(TimeSpan.FromSeconds(5));
            _disposed = true;
        }
    }
}
