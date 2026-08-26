using System.Text.Json;
using ZTR.Api.Extensions;

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

        app.MapZTREndpoints();

        app.Run();
    }
}