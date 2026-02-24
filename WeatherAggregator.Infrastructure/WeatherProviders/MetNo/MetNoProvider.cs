using System.Globalization;
using System.Net.Http.Json;
using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Application.Services;
using WeatherAggregator.Core;
using WeatherAggregator.Infrastructure.WeatherProviders.OpenMeteo;
using WeatherAggregator.Infrastructure.WeatherProviders.OpenMeteo.DTOs;

namespace WeatherAggregator.Infrastructure.WeatherProviders.MetNo;

public sealed class MetNoProvider : IWeatherProvider
{
    private readonly HttpClient _http;

    public MetNoProvider(HttpClient http)
    {
        _http = http;
    }

    public string Name => "MET Norway (api.met.no)";

    public async Task<Forecast> GetForecastAsync(GetWeatherForecastRequestDto request, CancellationToken cancellationToken)
    {
        var city = request.City.Trim();
        var country = request.Country.Trim();

        var geo = await _http.GetFromJsonAsync<OpenMeteoGeocodingResponse>(
            $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=en&format=json",
            cancellationToken);

        var loc = geo?.Results?.FirstOrDefault();
        if (loc is null)
            throw new InvalidOperationException($"Unable to geocode location '{city}, {country}'.");

        var lat = loc.Latitude.ToString(CultureInfo.InvariantCulture);
        var lon = loc.Longitude.ToString(CultureInfo.InvariantCulture);

        var url = $"weatherapi/locationforecast/2.0/compact?lat={lat}&lon={lon}";

        var response = await _http.GetFromJsonAsync<MetNoCompactResponse>(url, cancellationToken);

        var series = response?.Properties?.Timeseries;
        if (series is null || series.Count == 0)
            throw new InvalidOperationException("MET Norway API returned no timeseries data.");

        var desiredDate = request.Date;

        var candidates = series
            .Select(ts => new { Ts = ts, Date = DateOnly.FromDateTime(ts.Time.UtcDateTime) })
            .Where(x => x.Date == desiredDate)
            .ToList();

        if (candidates.Count == 0)
            throw new InvalidOperationException("MET Norway API has no forecast for the requested date (may be out of range).");

        var chosen = candidates
            .Select(x => new
            {
                x.Ts,
                HourDistance = Math.Abs(x.Ts.Time.UtcDateTime.Hour - 12)
            })
            .OrderBy(x => x.HourDistance)
            .First().Ts;

        var temp = chosen.Data?.Instant?.Details?.AirTemperature;
        if (temp is null)
            throw new InvalidOperationException("MET Norway API response did not include air_temperature.");

        var temperatureC = (int)Math.Round(temp.Value);

        return new Forecast(request.Date, city, country, temperatureC);
    }
}
