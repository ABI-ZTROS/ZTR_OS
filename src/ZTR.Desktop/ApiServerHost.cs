using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ZTR.Api.Extensions;
using ZTR.Api.Hubs;
using ZTR.Api.Middleware;

namespace ZTR.Desktop;

public class ApiServerHost : IDisposable
{
    private WebApplication? _app;
    private bool _disposed;

    public int Port { get; private set; }

    public async Task<bool> StartAsync()
    {
        foreach (int port in Enumerable.Range(5000, 11))
        {
            if (!IsPortAvailable(port)) continue;

            try
            {
                return await TryStartOnPort(port);
            }
            catch
            {
                continue;
            }
        }

        return false;
    }

    private async Task<bool> TryStartOnPort(int port)
    {
        Port = port;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>()
        });

        builder.WebHost.UseUrls($"http://localhost:{port}");
        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, port);
        });

        builder.Services.AddControllers()
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
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Desktop", policy =>
            {
                policy.WithOrigins("http://app.local", "http://localhost:5000")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        builder.Services.AddSignalR();
        builder.Services.AddHealthChecks();
        builder.Services.AddZTRServices();

        _app = builder.Build();

        _app.UseSwagger();
        _app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "ZTR_OS API v1");
        });

        _app.UseZTRMiddleware();
        _app.UseCors("Desktop");

        _app.MapZTREndpoints();

        await _app.StartAsync();

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
