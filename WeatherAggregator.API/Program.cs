using Asp.Versioning;
using WeatherAggregator.Application.Services;
using WeatherAggregator.Infrastructure;
using WeatherAggregator.Middleware;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WeatherAggregator.Health;
using WeatherAggregator.Infrastructure.Cache;
using WeatherAggregator.Infrastructure.WeatherProviders.XWeather;

namespace WeatherAggregator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        builder.Services.AddMemoryCache();

        builder.Services.Configure<WeatherForecastCacheOptions>(
            builder.Configuration.GetSection("WeatherForecastCache"));

        builder.Services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;

                // Route-parameter (URL segment) versioning only: /api/v{version}/...
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Clean Architecture DI
        builder.Services.AddInfrastructure();

        // Bind provider options
        builder.Services.Configure<XWeatherOptions>(
            builder.Configuration.GetSection("XWeather"));

        // Health checks
        var healthChecks = builder.Services.AddHealthChecks();

        // Register checks
        healthChecks.AddCheck("self", () => HealthCheckResult.Healthy("OK"), tags: new[] { "live" });

        // Provider checks (registration/config only)
        healthChecks.AddCheck<WeatherProviderHealthCheck>("providers", tags: new[] { "ready" });

        builder.Services.Configure<WeatherProviderHealthCheckOptions>(
            builder.Configuration.GetSection("HealthChecks:Providers"));

        var app = builder.Build();


        // Configure the HTTP request pipeline.
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready")
        });

        app.Run();
    }
}