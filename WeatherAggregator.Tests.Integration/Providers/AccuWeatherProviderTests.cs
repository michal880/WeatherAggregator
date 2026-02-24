using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Infrastructure.WeatherProviders.AccuWeather;

namespace WeatherAggregator.Tests.Integration.Providers;

public sealed class AccuWeatherProviderTests
{
    [Fact]
    public async Task GetForecastAsync_ReturnsForecast_ForKnownCity()
    {
        using var http = new HttpClient();
        http.BaseAddress = new Uri("https://www.accuweather.com/");
        http.DefaultRequestHeaders.Accept.ParseAdd("text/html");

        var provider = new AccuWeatherProvider(http);

        var request = new GetWeatherForecastRequestDto(
            Date: DateOnly.FromDateTime(DateTime.UtcNow.Date),
            City: "London",
            Country: "United Kingdom");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        var forecast = await provider.GetForecastAsync(request, cts.Token);

        Assert.Equal(request.Date, forecast.Date);
        Assert.Equal("London", forecast.City);
        Assert.NotNull(forecast.Country);
        Assert.InRange(forecast.TemperatureC!.Value, -80, 80);
    }
}

