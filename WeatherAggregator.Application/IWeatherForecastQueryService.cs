using WeatherAggregator.API.Contracts.WeatherForecast;

namespace WeatherAggregator.Application;

public interface IWeatherForecastQueryService
{
    Task<IEnumerable<WeatherForecastProviderResultDto>> GetForecastAsync(
        GetWeatherForecastRequestDto request,
        CancellationToken cancellationToken);
}

