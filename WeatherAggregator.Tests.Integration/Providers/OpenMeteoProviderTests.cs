using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Infrastructure.WeatherProviders.OpenMeteo;

namespace WeatherAggregator.Tests.Integration.Providers;

public class OpenMeteoProviderTests
{
    [Fact]
    public async Task GetForecastAsync_ReturnsForecast_ForKnownCity()
    {
        using (var http = new HttpClient())
        {
            http.BaseAddress = new Uri("https://api.open-meteo.com/");

            var provider = new OpenMeteoProvider(http);

            var request = new GetWeatherForecastRequestDto(
                Date: new DateOnly(2026, 2, 24),
                City: "London",
                Country: "United Kingdom");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var forecast = await provider.GetForecastAsync(request, cts.Token);

            Assert.Equal(request.Date, forecast.Date);
            Assert.Equal("London", forecast.City);
            Assert.NotNull(forecast.Country);
            Assert.InRange(forecast.TemperatureC!.Value, -80, 80);
        }
    }
}