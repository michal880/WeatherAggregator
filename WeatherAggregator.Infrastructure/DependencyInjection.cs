using System.Net;
using Polly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WeatherAggregator.Application;
using WeatherAggregator.Application.Services;
using WeatherAggregator.Infrastructure.Cache;
using WeatherAggregator.Infrastructure.WeatherProviders.AccuWeather;
using WeatherAggregator.Infrastructure.WeatherProviders.OpenMeteo;
using WeatherAggregator.Infrastructure.WeatherProviders.XWeather;

namespace WeatherAggregator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Providers
        // XWeather (API-based)
        services.AddOptions<XWeatherOptions>();

        static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
        {
            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r =>
                    r.StatusCode == HttpStatusCode.TooManyRequests ||
                    (int)r.StatusCode >= 500)
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1)));
        }

        services.AddHttpClient<XWeatherProvider>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<XWeatherOptions>>().Value;
            http.BaseAddress = new Uri(string.IsNullOrWhiteSpace(opts.BaseUrl) ? "https://api.aerisapi.com/" : opts.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(10);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("WeatherAggregator/1.0");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        })
        .AddPolicyHandler(CreateRetryPolicy());
        services.AddSingleton<IWeatherProvider>(sp => sp.GetRequiredService<XWeatherProvider>());

        // OpenMeteoProvider uses real HTTP calls, so it needs HttpClient.
        services.AddHttpClient<OpenMeteoProvider>(http =>
        {
            http.BaseAddress = new Uri("https://api.open-meteo.com/");
            http.Timeout = TimeSpan.FromSeconds(10);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("WeatherAggregator/1.0");
        })
        .AddPolicyHandler(CreateRetryPolicy());
        services.AddSingleton<IWeatherProvider>(sp => sp.GetRequiredService<OpenMeteoProvider>());

        services.AddHttpClient<AccuWeatherProvider>(http =>
        {
            http.BaseAddress = new Uri("https://www.accuweather.com/");
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("WeatherAggregator/1.0");
            http.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        })
        .AddPolicyHandler(CreateRetryPolicy());
        services.AddSingleton<IWeatherProvider>(sp => sp.GetRequiredService<AccuWeatherProvider>());

        services.AddSingleton<IWeatherForecastCache, MemoryWeatherForecastCache>();
        services.AddOptions<WeatherForecastCacheOptions>();

        // Application service can be registered here or in API; keeping it near provider wiring is convenient.
        services.AddSingleton<IWeatherForecastQueryService, WeatherForecastQueryService>();

        return services;
    }
}
