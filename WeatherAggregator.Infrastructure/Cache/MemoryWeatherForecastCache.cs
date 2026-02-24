using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Application.Services;
using WeatherAggregator.Core;

namespace WeatherAggregator.Infrastructure.Cache;

public sealed class MemoryWeatherForecastCache : IWeatherForecastCache
{
    private readonly IMemoryCache _cache;
    private readonly WeatherForecastCacheOptions _options;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> KeyLocks = new();

    public MemoryWeatherForecastCache(IMemoryCache cache, IOptions<WeatherForecastCacheOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public bool TryGet(GetWeatherForecastRequestDto request, out IReadOnlyList<WeatherForecastProviderResultDto>? results)
        => _cache.TryGetValue(BuildKey(request), out results);

    public void Set(GetWeatherForecastRequestDto request, IReadOnlyList<WeatherForecastProviderResultDto> results)
    {
        if (!ShouldCache(results))
            return;

        var ttl = _options.Ttl;
        if (ttl <= TimeSpan.Zero)
            return;

        _cache.Set(BuildKey(request), results, ttl);
    }

    public async Task<IReadOnlyList<WeatherForecastProviderResultDto>> GetOrCreateAsync(
        GetWeatherForecastRequestDto request,
        Func<CancellationToken, Task<IReadOnlyList<WeatherForecastProviderResultDto>>> factory,
        CancellationToken cancellationToken)
    {
        var key = BuildKey(request);

        if (_cache.TryGetValue(key, out IReadOnlyList<WeatherForecastProviderResultDto>? cached) && cached is not null)
        {
            if (_options.RefreshUnavailableInBackground && IsFullyUnavailable(cached))
            {
                _ = TriggerBackgroundRefreshAsync(key, factory);
            }

            return cached;
        }

        if (!_options.EnableInFlightDeduplication)
        {
            var created = await factory(cancellationToken);
            Set(request, created);
            return created;
        }

        var gate = KeyLocks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            if (_cache.TryGetValue(key, out cached) && cached is not null)
                return cached;

            var created = await factory(cancellationToken);
            Set(request, created);
            return created;
        }
        finally
        {
            gate.Release();

            if (gate.CurrentCount == 1)
                KeyLocks.TryRemove(key, out _);
        }
    }

    public bool RefreshIfUnavailable(
        GetWeatherForecastRequestDto request,
        Func<CancellationToken, Task<IReadOnlyList<WeatherForecastProviderResultDto>>> factory)
    {
        if (!_options.RefreshUnavailableInBackground)
            return false;

        var key = BuildKey(request);

        if (!_cache.TryGetValue(key, out IReadOnlyList<WeatherForecastProviderResultDto>? cached) || cached is null)
            return false;

        if (!IsFullyUnavailable(cached))
            return false;

        _ = TriggerBackgroundRefreshAsync(key, factory);
        return true;
    }

    private bool ShouldCache(IReadOnlyList<WeatherForecastProviderResultDto> results)
    {
        if (!_options.SkipCachingAllUnavailableResponses)
            return true;

        if (_options.RefreshUnavailableInBackground && IsFullyUnavailable(results))
            return true;

        return results.Any(r => r.Status == WeatherProviderStatus.Success);
    }

    private static string BuildKey(GetWeatherForecastRequestDto request)
        => $"forecast:{request.Date:yyyy-MM-dd}:{request.City.Trim().ToLowerInvariant()}:{request.Country.Trim().ToLowerInvariant()}";

    private static bool IsFullyUnavailable(IReadOnlyList<WeatherForecastProviderResultDto> results)
        => results.Count > 0 && results.All(r => r.Status == WeatherProviderStatus.Unavailable);

    private Task TriggerBackgroundRefreshAsync(
        string key,
        Func<CancellationToken, Task<IReadOnlyList<WeatherForecastProviderResultDto>>> factory)
    {
        void SetFromRefresh(IReadOnlyList<WeatherForecastProviderResultDto> results)
        {
            var ttl = _options.Ttl;
            if (ttl <= TimeSpan.Zero)
                return;

            _cache.Set(key, results, ttl);
        }

        if (!_options.EnableInFlightDeduplication)
        {
            return Task.Run(async () =>
            {
                var created = await factory(CancellationToken.None);
                SetFromRefresh(created);
            });
        }

        var gate = KeyLocks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));

        return Task.Run(async () =>
        {
            if (!await gate.WaitAsync(TimeSpan.FromSeconds(1)))
                return;

            try
            {
                if (_cache.TryGetValue(key, out IReadOnlyList<WeatherForecastProviderResultDto>? current) &&
                    current is not null &&
                    !IsFullyUnavailable(current))
                {
                    return;
                }

                var created = await factory(CancellationToken.None);
                SetFromRefresh(created);
            }
            finally
            {
                gate.Release();

                if (gate.CurrentCount == 1)
                    KeyLocks.TryRemove(key, out _);
            }
        });
    }
}
