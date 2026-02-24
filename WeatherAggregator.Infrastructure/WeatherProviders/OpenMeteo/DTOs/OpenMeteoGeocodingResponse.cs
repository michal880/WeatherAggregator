using System.Text.Json.Serialization;

namespace WeatherAggregator.Infrastructure.WeatherProviders.OpenMeteo.DTOs;

public sealed record OpenMeteoGeocodingResponse(
    [property: JsonPropertyName("results")] List<OpenMeteoGeoResult>? Results);
