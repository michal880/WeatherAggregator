using WeatherAggregator.API.Contracts.WeatherForecast;

namespace WeatherAggregator.Application.Services;

public interface IWeatherForecastCache
{
    bool TryGet(GetWeatherForecastRequestDto request, out IReadOnlyList<WeatherForecastProviderResultDto>? results);
    Task<IReadOnlyList<WeatherForecastProviderResultDto>> GetOrCreateAsync(
        GetWeatherForecastRequestDto request,
        Func<CancellationToken, Task<IReadOnlyList<WeatherForecastProviderResultDto>>> factory,
        CancellationToken cancellationToken);
    bool RefreshIfUnavailable(
        GetWeatherForecastRequestDto request,
        Func<CancellationToken, Task<IReadOnlyList<WeatherForecastProviderResultDto>>> factory);
}
