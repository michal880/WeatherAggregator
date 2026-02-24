namespace WeatherAggregator.Core;

public record EmptyForecast(
    DateOnly Date,
    string City,
    string Country) : Forecast(Date, City, Country, null)
{ }