using System.IO;
using System.Net;
using System.Text.Json;

namespace ZTR.Api.Middleware;

public class IntegrationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IntegrationMiddleware> _logger;

    private static readonly Dictionary<string, ValueRange> _validRanges = new()
    {
        ["temperature"] = new ValueRange(0, 120),
        ["usage"] = new ValueRange(0, 100),
        ["power"] = new ValueRange(0, 2000),
        ["clock"] = new ValueRange(0, 10000),
        ["fanspeed"] = new ValueRange(0, 100),
        ["fanrpm"] = new ValueRange(0, 5000),
        ["charge"] = new ValueRange(0, 100),
        ["brightness"] = new ValueRange(0, 100),
        ["speed"] = new ValueRange(0, 100),
        ["splash"] = new ValueRange(0, 100),
        ["ripple"] = new ValueRange(0, 100),
    };

    public IntegrationMiddleware(RequestDelegate next, ILogger<IntegrationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == HttpMethods.Post ||
            context.Request.Method == HttpMethods.Put)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            if (IsKnownWriteEndpoint(path))
            {
                var body = await ReadBodyAsync(context.Request);
                if (!string.IsNullOrEmpty(body))
                {
                    var validationResult = ValidateRequest(body, path);
                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning(
                            "Rejected invalid request to {Path}: {Reason}",
                            context.Request.Path, validationResult.Reason);

                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        context.Response.ContentType = "application/json";

                        var response = new
                        {
                            success = false,
                            error = validationResult.Reason
                        };

                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(response));
                        return;
                    }
                }
            }
        }

        await _next(context);
    }

    private static bool IsKnownWriteEndpoint(string path)
    {
        return path.Contains("fan-curves") ||
               path.Contains("power-limits") ||
               path.Contains("mode") ||
               path.Contains("aura") ||
               path.Contains("binding") ||
               path.Contains("settings") ||
               path.Contains("mlp/config") ||
               path.Contains("hardware");
    }

    private static async Task<string> ReadBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static ValidationResult ValidateRequest(string body, string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            foreach (var property in root.EnumerateObject())
            {
                var name = property.Name.ToLowerInvariant();

                if (_validRanges.TryGetValue(name, out var range))
                {
                    if (property.Value.ValueKind == JsonValueKind.Number)
                    {
                        var value = property.Value.GetDouble();
                        if (value < range.Min || value > range.Max)
                        {
                            return ValidationResult.Invalid(
                                $"Value '{property.Name}' is {value}, outside valid range [{range.Min}-{range.Max}]");
                        }
                    }
                }

                if (property.Value.ValueKind == JsonValueKind.Number &&
                    (name.Contains("temp") || name.Contains("temperature")))
                {
                    var value = property.Value.GetDouble();
                    if (value < _validRanges["temperature"].Min ||
                        value > _validRanges["temperature"].Max)
                    {
                        return ValidationResult.Invalid(
                            $"Temperature value '{property.Name}' is {value}, outside valid range [0-120]");
                    }
                }

                if (property.Value.ValueKind == JsonValueKind.Number &&
                    (name.Contains("speed")) && !name.Contains("fan"))
                {
                    var value = property.Value.GetDouble();
                    if (value < _validRanges["fanspeed"].Min ||
                        value > _validRanges["fanspeed"].Max)
                    {
                        return ValidationResult.Invalid(
                            $"Speed value '{property.Name}' is {value}, outside valid range [0-100]");
                    }
                }

                if (property.Value.ValueKind == JsonValueKind.Number &&
                    name.Contains("limit"))
                {
                    var value = property.Value.GetDouble();
                    if (value < _validRanges["power"].Min ||
                        value > _validRanges["power"].Max)
                    {
                        return ValidationResult.Invalid(
                            $"Power limit '{property.Name}' is {value}, outside valid range [0-2000]");
                    }
                }

                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    var nested = ValidateRequest(property.Value.GetRawText(), path);
                    if (!nested.IsValid)
                        return nested;
                }
            }

            return ValidationResult.Valid();
        }
        catch (JsonException)
        {
            return ValidationResult.Valid();
        }
    }

    private readonly record struct ValueRange(double Min, double Max);

    private readonly struct ValidationResult
    {
        public bool IsValid { get; }
        public string Reason { get; }

        private ValidationResult(bool isValid, string reason)
        {
            IsValid = isValid;
            Reason = reason;
        }

        public static ValidationResult Valid() => new(true, string.Empty);
        public static ValidationResult Invalid(string reason) => new(false, reason);
    }
}