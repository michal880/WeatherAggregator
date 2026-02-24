namespace WeatherAggregator.API.Contracts.WeatherForecast;

public sealed record GetWeatherForecastRequestDto(
    DateOnly Date,
    string City,
    string Country);
