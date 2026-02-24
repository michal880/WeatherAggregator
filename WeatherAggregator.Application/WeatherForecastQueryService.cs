using Microsoft.Extensions.Logging;
using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Application.Services;
using WeatherAggregator.Application.Validation;
using WeatherAggregator.Core;

namespace WeatherAggregator.Application;

public sealed class WeatherForecastQueryService : IWeatherForecastQueryService
{
    private readonly IReadOnlyList<IWeatherProvider> _providers;
    private readonly ILogger<WeatherForecastQueryService> _logger;
     private readonly IWeatherForecastCache _cache;

    public WeatherForecastQueryService(
        IEnumerable<IWeatherProvider> providers,
        IWeatherForecastCache cache,
        ILogger<WeatherForecastQueryService> logger)
    {
        _providers = providers.ToList();
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<WeatherForecastProviderResultDto>> GetForecastAsync(GetWeatherForecastRequestDto request, CancellationToken cancellationToken)
    {
        ForecastRequestValidator.Validate(request);

        if (_providers.Count < 3)
            throw new InvalidOperationException("At least 3 weather providers must be registered.");

        using var _ = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["City"] = request.City,
            ["Country"] = request.Country,
            ["Date"] = request.Date
        });

        // If we already have a cached value, return it immediately, but try a background refresh when it's fully unavailable.
        if (_cache.TryGet(request, out var cached) && cached is not null)
        {
            var triggered = _cache.RefreshIfUnavailable(request, ct => FetchFromProvidersAsync(request, ct));
            if (triggered)
                _logger.LogInformation("Cached forecast is unavailable; background refresh triggered");

            _logger.LogInformation("Forecast cache hit");
            return cached;
        }

        return await _cache.GetOrCreateAsync(request, ct => FetchFromProvidersAsync(request, ct), cancellationToken);
    }

    private async Task<IReadOnlyList<WeatherForecastProviderResultDto>> FetchFromProvidersAsync(
        GetWeatherForecastRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Forecast cache miss");

        var startedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation("Forecast request started");

        var weatherTasks = _providers.Select(p => SafeGetAsync(p, request, cancellationToken)).ToArray();
        var results = await Task.WhenAll(weatherTasks);

        var durationMs = (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
        _logger.LogInformation("Forecast request completed in {DurationMs}ms", (long)durationMs);

        return results.ToList();
    }

    private async Task<WeatherForecastProviderResultDto> SafeGetAsync(
        IWeatherProvider provider,
        GetWeatherForecastRequestDto request,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            var forecast = await provider.GetForecastAsync(request, cancellationToken);

            var durationMs = (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
            _logger.LogInformation("Weather provider {Provider} succeeded in {DurationMs}ms", provider.Name, (long)durationMs);

            return new WeatherForecastProviderResultDto(
                Provider: provider.Name,
                Status: WeatherProviderStatus.Success,
                Forecast: forecast.ToWeatherForecastDto());
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Weather provider {Provider} canceled", provider.Name);
            throw;
        }
        catch (Exception ex)
        {
            var durationMs = (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
            _logger.LogWarning(ex, "Weather provider {Provider} unavailable after {DurationMs}ms", provider.Name, (long)durationMs);

            var fallbackForecastDto = new WeatherForecastDto(
                Date: request.Date,
                City: request.City.Trim(),
                Country: request.Country.Trim(),
                TemperatureC: null);

            return new WeatherForecastProviderResultDto(
                Provider: provider.Name,
                Status: WeatherProviderStatus.Unavailable,
                Forecast: fallbackForecastDto);
        }
    }
}
