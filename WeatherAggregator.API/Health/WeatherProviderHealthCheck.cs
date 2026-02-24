using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Application.Services;

namespace WeatherAggregator.Health;

public sealed class WeatherProviderHealthCheck : IHealthCheck
{
    private readonly IEnumerable<IWeatherProvider> _providers;
    private readonly WeatherProviderHealthCheckOptions _options;

    public WeatherProviderHealthCheck(
        IEnumerable<IWeatherProvider> providers,
        IOptions<WeatherProviderHealthCheckOptions> options)
    {
        _providers = providers;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var providers = _providers.ToList();
        var providerNames = providers.Select(p => p.Name).Distinct().Order().ToArray();

        var perProvider = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (_options.EnablePing)
        {
            var pingRequest = new GetWeatherForecastRequestDto(
                Date: DateOnly.FromDateTime(DateTime.UtcNow.Date),
                City: _options.PingCity,
                Country: _options.PingCountry);

            foreach (var p in providers)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_options.ProviderTimeout);

                try
                {
                    await p.GetForecastAsync(pingRequest, cts.Token);
                    perProvider[p.Name] = "Healthy";
                }
                catch (OperationCanceledException)
                {
                    perProvider[p.Name] = "Timeout";
                }
                catch (Exception ex)
                {
                    // Don't fail readiness because a third-party site might be flaky or block us.
                    perProvider[p.Name] = $"Failed: {ex.GetType().Name}";
                }
            }
        }

        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            ["providerCount"] = providerNames.Length,
            ["providers"] = providerNames,
            ["pingEnabled"] = _options.EnablePing,
            ["pingCity"] = _options.PingCity,
            ["pingCountry"] = _options.PingCountry,
            ["providerTimeout"] = _options.ProviderTimeout.ToString(),
            ["perProvider"] = perProvider
        };

        if (providerNames.Length < 3)
            return HealthCheckResult.Unhealthy("Less than 3 providers registered", data: data);

        // If pinging is enabled and at least one provider fails, mark as Degraded.
        if (_options.EnablePing && perProvider.Values.Any(v => !string.Equals(v?.ToString(), "Healthy", StringComparison.OrdinalIgnoreCase)))
            return HealthCheckResult.Degraded("One or more providers failed ping", data: data);

        return HealthCheckResult.Healthy("Providers registered", data);
    }
}
