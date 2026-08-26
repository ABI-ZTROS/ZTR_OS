using System.Text.Json;
using ZTR.Api.Extensions;
using ZTR.Api.Hubs;

namespace ZTR.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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
                Title = "ZTR_OS API",
                Version = "v1",
                Description = "ZTR_OS Backend API for ASUS ROG device control and AI optimization"
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

        ConfigureUrls(builder, args);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ZTR_OS API v1");
            });
        }

        app.UseZTRMiddleware();

        app.UseHttpsRedirection();

        app.UseCors("AllowAll");

        var defaultFileOptions = new DefaultFilesOptions();
        defaultFileOptions.DefaultFileNames.Clear();
        defaultFileOptions.DefaultFileNames.Add("index.html");
        app.UseDefaultFiles(defaultFileOptions);

        var staticFileOptions = new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (ctx.Context.Request.Path.StartsWithSegments("/assets"))
                {
                    ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                }
            }
        };
        app.UseStaticFiles(staticFileOptions);

        app.MapZTREndpoints();

        app.MapFallbackToFile("index.html");

        app.Run();
    }

    private static void ConfigureUrls(WebApplicationBuilder builder, string[] args)
    {
        var urls = new List<string>();

        // 1. Check command line --urls argument
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--urls" && i + 1 < args.Length)
            {
                urls.AddRange(args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries));
                break;
            }
        }

        // 2. Check ASPNETCORE_URLS environment variable
        var envUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (!string.IsNullOrEmpty(envUrls))
        {
            urls.AddRange(envUrls.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        // 3. Default: try port 5000, then 5001-5010
        if (urls.Count == 0)
        {
            foreach (var port in Enumerable.Range(5000, 11))
            {
                if (IsPortAvailable(port))
                {
                    urls.Add($"http://localhost:{port}");
                    Console.WriteLine($"[ZTR_OS] Using port {port}");
                    break;
                }
                else
                {
                    Console.WriteLine($"[ZTR_OS] Port {port} is in use, trying next...");
                }
            }
        }

        if (urls.Count > 0)
        {
            builder.WebHost.UseUrls(urls.ToArray());
        }
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
