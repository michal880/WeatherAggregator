using System.Text.Json.Serialization;

namespace WeatherAggregator.Infrastructure.WeatherProviders.OpenMeteo.DTOs;

public sealed record OpenMeteoDaily(
    [property: JsonPropertyName("temperature_2m_max")] List<double?>? Temperature2mMax);
