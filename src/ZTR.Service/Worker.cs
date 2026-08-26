using ZTR.Api.Extensions;

namespace ZTR.Service;

public class Worker : BackgroundService
{
    private WebApplication? _app;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ZTR_OS Service starting...");

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddControllers();
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

        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(System.Net.IPAddress.Loopback, 5000);
        });

        _app = builder.Build();

        _app.UseSwagger();
        _app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "ZTR_OS API v1");
        });

        _app.UseZTRMiddleware();
        _app.UseCors("AllowAll");
        _app.MapZTREndpoints();

        _logger.LogInformation("ZTR_OS Service listening on http://localhost:5000");

        await _app.RunAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ZTR_OS Service stopping...");

        if (_app != null)
        {
            await _app.StopAsync(cancellationToken);
            ((IHost)_app).Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}