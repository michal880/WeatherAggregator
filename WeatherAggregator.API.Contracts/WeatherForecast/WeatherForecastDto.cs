namespace WeatherAggregator.API.Contracts.WeatherForecast;

public sealed record WeatherForecastDto(
    DateOnly Date,
    string City,
    string Country,
    int? TemperatureC);
