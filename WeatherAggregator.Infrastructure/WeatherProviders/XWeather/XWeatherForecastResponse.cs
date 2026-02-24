using System.Text.Json.Serialization;

namespace WeatherAggregator.Infrastructure.WeatherProviders.XWeather;

public sealed record XWeatherForecastResponse(
    [property: JsonPropertyName("response")] List<XWeatherForecastLocationResponse>? Response);

public sealed record XWeatherForecastLocationResponse(
    [property: JsonPropertyName("periods")] List<XWeatherForecastPeriod>? Periods);

public sealed record XWeatherForecastPeriod(
    [property: JsonPropertyName("maxTempC")] double? MaxTempC,
    [property: JsonPropertyName("avgTempC")] double? AvgTempC,
    [property: JsonPropertyName("minTempC")] double? MinTempC);
