using System.Text.Json.Serialization;

namespace WeatherAggregator.Infrastructure.WeatherProviders.MetNo;

public sealed record MetNoCompactResponse(
    [property: JsonPropertyName("properties")] MetNoProperties? Properties);

public sealed record MetNoProperties(
    [property: JsonPropertyName("timeseries")] List<MetNoTimeseries>? Timeseries);

public sealed record MetNoTimeseries(
    [property: JsonPropertyName("time")] DateTimeOffset Time,
    [property: JsonPropertyName("data")] MetNoData? Data);

public sealed record MetNoData(
    [property: JsonPropertyName("instant")] MetNoInstant? Instant);

public sealed record MetNoInstant(
    [property: JsonPropertyName("details")] MetNoInstantDetails? Details);

public sealed record MetNoInstantDetails(
    [property: JsonPropertyName("air_temperature")] double? AirTemperature);

