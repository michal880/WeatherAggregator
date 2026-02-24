using System.Globalization;
using AngleSharp;
using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Application.Services;
using WeatherAggregator.Core;

namespace WeatherAggregator.Infrastructure.WeatherProviders.AccuWeather;

public sealed class AccuWeatherProvider : IWeatherProvider
{
    private readonly HttpClient _http;

    public AccuWeatherProvider(HttpClient http)
    {
        _http = http;
    }

    public string Name => "AccuWeather";

    public async Task<Forecast> GetForecastAsync(GetWeatherForecastRequestDto request, CancellationToken cancellationToken)
    {
        var city = request.City.Trim();
        var country = request.Country.Trim();

        var context = BrowsingContext.New(Configuration.Default);

        var searchUrl = $"https://www.accuweather.com/en/search-locations?query={Uri.EscapeDataString(city)}";

        using var searchReq = new HttpRequestMessage(HttpMethod.Get, searchUrl);
        AddBrowserLikeHeaders(searchReq);

        using var searchResp = await _http.SendAsync(searchReq, cancellationToken);
        searchResp.EnsureSuccessStatusCode();
        var searchHtml = await searchResp.Content.ReadAsStringAsync(cancellationToken);

        var searchDoc = await context.OpenAsync(req => req.Content(searchHtml), cancellationToken);

        var href = searchDoc
            .QuerySelectorAll("a")
            .Select(a => a.GetAttribute("href"))
            .FirstOrDefault(h =>
                !string.IsNullOrWhiteSpace(h) &&
                h.Contains("/daily-weather-forecast/", StringComparison.OrdinalIgnoreCase));

        var dailyUrl = NormalizeAccuWeatherUrl(href);
        if (string.IsNullOrWhiteSpace(dailyUrl))
            throw new InvalidOperationException("Unable to find an AccuWeather daily forecast URL from search results.");

        if (!dailyUrl.Contains("metric", StringComparison.OrdinalIgnoreCase))
            dailyUrl += (dailyUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?") + "metric=true";

        using var pageReq = new HttpRequestMessage(HttpMethod.Get, dailyUrl);
        AddBrowserLikeHeaders(pageReq);

        using var pageResp = await _http.SendAsync(pageReq, cancellationToken);
        pageResp.EnsureSuccessStatusCode();
        var html = await pageResp.Content.ReadAsStringAsync(cancellationToken);

        var doc = await context.OpenAsync(req => req.Content(html), cancellationToken);

        var tempTextCandidates = doc
            .QuerySelectorAll(".temp, .temperature")
            .Select(e => e.TextContent)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        int? tempC = null;
        foreach (var t in tempTextCandidates)
        {
            tempC = AccuWeatherTemperatureParser.TryParseTemperatureC(t);
            if (tempC is not null)
                break;
        }

        if (tempC is null)
        {
            foreach (var script in doc.QuerySelectorAll("script").Select(s => s.TextContent))
            {
                tempC = AccuWeatherTemperatureParser.TryParseTemperatureC(script);
                if (tempC is not null)
                    break;
            }
        }

        if (tempC is null)
            tempC = AccuWeatherTemperatureParser.TryParseTemperatureC(doc.Body?.TextContent ?? string.Empty);

        if (tempC is null)
            throw new InvalidOperationException("Unable to parse temperature from AccuWeather HTML.");

        return new Forecast(request.Date, city, country, tempC.Value);
    }

    private static void AddBrowserLikeHeaders(HttpRequestMessage req)
    {
        req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
        req.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        req.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    }

    private static string NormalizeAccuWeatherUrl(string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
            return string.Empty;

        if (href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return href;

        if (href.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            return $"https://www.accuweather.com{href}";

        return $"https://www.accuweather.com/{href}";
    }
}
