using WeatherAggregator.Core;

namespace WeatherAggregator.API.Contracts.WeatherForecast;

public sealed record WeatherForecastProviderResultDto(
    string Provider,
    WeatherProviderStatus Status,
    WeatherForecastDto Forecast);