using WeatherAggregator.API.Contracts.WeatherForecast;
namespace WeatherAggregator.Application.Validation;

public static class ForecastRequestValidator
{
    public static void Validate(GetWeatherForecastRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.City))
            throw new ValidationException("city is required.");

        if (string.IsNullOrWhiteSpace(request.Country))
            throw new ValidationException("country is required.");
    }
}
