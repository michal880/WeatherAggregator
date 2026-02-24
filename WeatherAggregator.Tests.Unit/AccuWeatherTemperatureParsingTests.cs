using WeatherAggregator.Infrastructure.WeatherProviders.AccuWeather;

namespace WeatherAggregator.Tests.Unit;

public sealed class AccuWeatherTemperatureParsingTests
{
    [Theory]
    [InlineData("72°F", 22)]
    [InlineData("32°F", 0)]
    [InlineData("-4°F", -20)]
    [InlineData("22°C", 22)]
    [InlineData("22° C", 22)]
    [InlineData("15°", 15)]
    [InlineData("41°", 5)]
    public void TryParseTemperatureC_ParsesAndConvertsAsExpected(string input, int expectedC)
    {
        Assert.Equal(expectedC, AccuWeatherTemperatureParser.TryParseTemperatureC(input));
    }
}
