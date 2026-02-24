using System.Globalization;
using System.Text.RegularExpressions;

namespace WeatherAggregator.Infrastructure.WeatherProviders.AccuWeather;

internal static class AccuWeatherTemperatureParser
{
    internal static int? TryParseTemperatureC(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var match = Regex.Match(
            text,
            @"(?<!\d)(-?\d{1,3})\s*°\s*(?<unit>[cCfF])?",
            RegexOptions.CultureInvariant);

        if (!match.Success)
            return null;

        if (!int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return null;

        var unit = match.Groups["unit"].Value;

        if (string.Equals(unit, "F", StringComparison.OrdinalIgnoreCase))
            return (int)Math.Round((value - 32) * 5.0 / 9.0);

        if (string.Equals(unit, "C", StringComparison.OrdinalIgnoreCase))
            return value;

        if (value > 35)
            return (int)Math.Round((value - 32) * 5.0 / 9.0);

        return value;
    }
}

