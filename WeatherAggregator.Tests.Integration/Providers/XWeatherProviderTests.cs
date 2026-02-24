using Microsoft.Extensions.Configuration;
using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Infrastructure.WeatherProviders.XWeather;
using Xunit;

namespace WeatherAggregator.Tests.Integration.Providers;

public sealed class XWeatherProviderTests
{
    [Fact]
    public async Task GetForecastAsync_ReturnsForecast_ForKnownCity_FromTestConfigOrEnv()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: true)
            .Build();

        var opts = new XWeatherOptions();
        config.GetSection("XWeather").Bind(opts);

        if (string.IsNullOrWhiteSpace(opts.ClientId) || string.IsNullOrWhiteSpace(opts.ClientSecret))
            return;

        using var http = new HttpClient { BaseAddress = new Uri(opts.BaseUrl) };

        var provider = new XWeatherProvider(http, new Microsoft.Extensions.Options.OptionsWrapper<XWeatherOptions>(opts));

        var request = new GetWeatherForecastRequestDto(
            Date: DateOnly.FromDateTime(DateTime.UtcNow.Date),
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
