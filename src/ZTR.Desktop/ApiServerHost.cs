using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
                var fileProvider = new PhysicalFileProvider(wwwrootPath);

                _app.UseMiddleware<ApiUrlInjectorMiddleware>(port);

                var defaultFileOptions = new DefaultFilesOptions
                {
                    FileProvider = fileProvider
                };
                defaultFileOptions.DefaultFileNames.Clear();
                defaultFileOptions.DefaultFileNames.Add("index.html");
                _app.UseDefaultFiles(defaultFileOptions);

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

public class ApiUrlInjectorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly int _port;

    public ApiUrlInjectorMiddleware(RequestDelegate next, int port)
    {
        _next = next;
        _port = port;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/" || context.Request.Path == "/index.html")
        {
            string baseUrl = $"http://localhost:{_port}";

            var originalBody = context.Response.Body;
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await _next(context);

            context.Response.Body = originalBody;

            if (context.Response.ContentType?.Contains("text/html") == true)
            {
                memoryStream.Position = 0;
                using var reader = new StreamReader(memoryStream);
                string html = await reader.ReadToEndAsync();

                string injection = $"<script>window.__API_BASE_URL__='{baseUrl}';</script>";
                if (!html.Contains("window.__API_BASE_URL__"))
                {
                    html = html.Replace("</head>", $"{injection}</head>");
                }

                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(html);
                context.Response.ContentLength = bytes.Length;
                await context.Response.Body.WriteAsync(new ReadOnlyMemory<byte>(bytes));
            }
            else
            {
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(context.Response.Body);
            }
        }
        else
        {
            await _next(context);
        }
    }
}
