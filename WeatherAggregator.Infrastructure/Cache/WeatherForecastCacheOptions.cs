namespace WeatherAggregator.Infrastructure.Cache;

public sealed class WeatherForecastCacheOptions
{
    public TimeSpan Ttl { get; init; } = TimeSpan.FromMinutes(10);

    public bool EnableInFlightDeduplication { get; init; } = true;

    public bool SkipCachingAllUnavailableResponses { get; init; } = true;

    public bool RefreshUnavailableInBackground { get; init; } = true;
}
