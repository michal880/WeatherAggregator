using System.Text.Json.Serialization;

namespace WeatherAggregator.Infrastructure.WeatherProviders.OpenMeteo.DTOs;

public sealed record OpenMeteoForecastResponse(
    [property: JsonPropertyName("daily")] OpenMeteoDaily? Daily);

