namespace WeatherAggregator.Core;

public record Forecast(
    DateOnly Date,
    string City,
    string Country,
    int? TemperatureC);

