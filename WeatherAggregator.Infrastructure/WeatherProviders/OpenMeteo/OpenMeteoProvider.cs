using System.Globalization;
using System.Net.Http.Json;
using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Application.Services;
using WeatherAggregator.Core;
using WeatherAggregator.Infrastructure.WeatherProviders.OpenMeteo.DTOs;

namespace WeatherAggregator.Infrastructure.WeatherProviders.OpenMeteo;

public sealed class OpenMeteoProvider : IWeatherProvider
{
    private readonly HttpClient _http;

    public OpenMeteoProvider(HttpClient http)
    {
        _http = http;
    }

    public string Name => "Open-Meteo";

    private string? ProviderUrl => "https://open-meteo.com/";

    public async Task<Forecast> GetForecastAsync(GetWeatherForecastRequestDto request, CancellationToken cancellationToken)
    {
        var city = request.City.Trim();
        var country = request.Country.Trim();

        var geo = await _http.GetFromJsonAsync<OpenMeteoGeocodingResponse>(
            $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=en&format=json",
            cancellationToken);

        var loc = geo?.Results?.FirstOrDefault(r =>
            string.Equals(r.Name, city, StringComparison.OrdinalIgnoreCase) ||
            (r.Country != null && string.Equals(r.Country, country, StringComparison.OrdinalIgnoreCase)));

        if (loc is null)
            throw new InvalidOperationException($"Unable to geocode location '{city}, {country}'.");

        var dateString = request.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var forecast = await _http.GetFromJsonAsync<OpenMeteoForecastResponse>(
            $"v1/forecast?latitude={loc.Latitude.ToString(CultureInfo.InvariantCulture)}&longitude={loc.Longitude.ToString(CultureInfo.InvariantCulture)}&daily=temperature_2m_max&timezone=UTC&start_date={dateString}&end_date={dateString}",
            cancellationToken);

        var temp = forecast?.Daily?.Temperature2mMax?.FirstOrDefault();

        if (temp is null)
            throw new InvalidOperationException("Forecast data not available for the requested date/location.");

        return new Forecast(request.Date, city, country, (int)Math.Round(temp.Value));
    }
}
