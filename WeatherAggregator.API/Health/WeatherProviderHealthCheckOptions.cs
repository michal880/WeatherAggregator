namespace WeatherAggregator.Health;

public sealed class WeatherProviderHealthCheckOptions
{
    public bool EnablePing { get; init; }

    public TimeSpan ProviderTimeout { get; init; } = TimeSpan.FromSeconds(2);

    public string PingCity { get; init; } = "London";

    public string PingCountry { get; init; } = "United Kingdom";
}
