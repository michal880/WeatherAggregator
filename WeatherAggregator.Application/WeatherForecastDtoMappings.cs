using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Core;

namespace WeatherAggregator.Application;

public static class WeatherForecastDtoMappings
{
    public static WeatherForecastDto ToWeatherForecastDto(this Forecast forecast)
        => new(
            Date: forecast.Date,
            City: forecast.City,
            Country: forecast.Country,
            TemperatureC: forecast.TemperatureC);
}
