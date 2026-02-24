namespace WeatherAggregator.Infrastructure.WeatherProviders.XWeather;

public sealed class XWeatherOptions
{
    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "https://api.aerisapi.com/";
}
