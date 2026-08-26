using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using ZTR.Api.Extensions;
using ZTR.Api.Hubs;
using ZTR.Api.Middleware;

namespace ZTR.Desktop;

public class ApiServerHost : IDisposable
{
    private WebApplication? _app;
    private bool _disposed;

    public int Port { get; private set; }

    public async Task<bool> StartAsync(int port)
    {
        try
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
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
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
            _app.UseCors("AllowAll");

            string wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            if (Directory.Exists(wwwrootPath))
            {
                var defaultFileOptions = new DefaultFilesOptions();
                defaultFileOptions.DefaultFileNames.Clear();
                defaultFileOptions.DefaultFileNames.Add("index.html");
                _app.UseDefaultFiles(defaultFileOptions);

                var fileProvider = new PhysicalFileProvider(wwwrootPath);
                var staticFileOptions = new StaticFileOptions
                {
                    FileProvider = fileProvider,
                    OnPrepareResponse = ctx =>
                    {
                        if (ctx.Context.Request.Path.StartsWithSegments("/assets"))
                        {
                            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                        }
                    }
                };
                _app.UseStaticFiles(staticFileOptions);
            }

            _app.MapZTREndpoints();
            _app.MapFallbackToFile("index.html", Path.Combine(wwwrootPath, "index.html"));

            await _app.StartAsync();

            return true;
        }
        catch (Exception)
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
