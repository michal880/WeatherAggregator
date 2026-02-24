using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Application.Services;
using WeatherAggregator.Core;
using WeatherAggregator.Infrastructure.WeatherProviders.XWeather;

namespace WeatherAggregator.Infrastructure.WeatherProviders.XWeather;

public sealed class XWeatherProvider : IWeatherProvider
{
    private readonly HttpClient _http;
    private readonly XWeatherOptions _options;

    public XWeatherProvider(HttpClient http, IOptions<XWeatherOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public string Name => "XWeather";

    public async Task<Forecast> GetForecastAsync(GetWeatherForecastRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new InvalidOperationException("XWeather credentials are not configured. Set XWeather:ClientId and XWeather:ClientSecret.");

        var city = request.City.Trim();
        var country = request.Country.Trim();

        // XWeather endpoint/response shapes vary by plan; this is a best-effort daily max temp extraction.

        var location = Uri.EscapeDataString($"{city},{country}");

        var date = request.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var path = $"forecasts/{location}?from={date}&to={date}&client_id={Uri.EscapeDataString(_options.ClientId)}&client_secret={Uri.EscapeDataString(_options.ClientSecret)}";

        using var resp = await _http.GetAsync(path, cancellationToken);
        if (resp.StatusCode == HttpStatusCode.Unauthorized || resp.StatusCode == HttpStatusCode.Forbidden)
            throw new InvalidOperationException("XWeather request failed due to invalid credentials or insufficient permissions.");

        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<XWeatherForecastResponse>(cancellationToken: cancellationToken);

        var period = payload?.Response?.FirstOrDefault()?.Periods?.FirstOrDefault();
        var tempC = period?.MaxTempC ?? period?.AvgTempC ?? period?.MinTempC;

        if (tempC is null)
            throw new InvalidOperationException("XWeather response did not include temperature data.");

        return new Forecast(request.Date, city, country, (int)Math.Round((double)tempC.Value));
    }
}
