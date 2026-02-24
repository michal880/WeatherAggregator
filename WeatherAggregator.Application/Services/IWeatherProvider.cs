using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Core;

namespace WeatherAggregator.Application.Services;

public interface IWeatherProvider
{
    string Name { get; }

    Task<Forecast> GetForecastAsync(GetWeatherForecastRequestDto request, CancellationToken cancellationToken);
}

