using System.Diagnostics;

namespace ZTR.Api.Middleware;

public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;

    public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        await _next(context);

        sw.Stop();

        var elapsed = sw.ElapsedMilliseconds;
        if (elapsed > 500)
        {
            _logger.LogWarning("Slow request: {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode})",
                context.Request.Method, context.Request.Path, elapsed, context.Response.StatusCode);
        }
        else
        {
            _logger.LogDebug("Request: {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode})",
                context.Request.Method, context.Request.Path, elapsed, context.Response.StatusCode);
        }
    }
}