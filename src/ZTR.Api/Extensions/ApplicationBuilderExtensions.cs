using ZTR.Api.Hubs;
using ZTR.Api.Middleware;

namespace ZTR.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseZTRMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseMiddleware<IntegrationMiddleware>();
        app.UseMiddleware<PerformanceLoggingMiddleware>();

        return app;
    }

    public static IEndpointRouteBuilder MapZTREndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
        endpoints.MapHub<SensorHub>("/hubs/sensor");
        endpoints.MapHub<StateHub>("/hubs/state");
        endpoints.MapHub<HardwareDataHub>("/hubs/hardware");
        endpoints.MapHealthChecks("/health");

        return endpoints;
    }
}