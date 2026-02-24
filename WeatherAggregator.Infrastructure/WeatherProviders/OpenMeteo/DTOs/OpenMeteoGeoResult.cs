using System.Text.Json.Serialization;

namespace WeatherAggregator.Infrastructure.WeatherProviders.OpenMeteo.DTOs;

public sealed record OpenMeteoGeoResult(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude);
