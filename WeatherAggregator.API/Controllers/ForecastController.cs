using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WeatherAggregator.API.Contracts.WeatherForecast;
using WeatherAggregator.Application;

namespace WeatherAggregator.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/forecasts")]
public sealed class ForecastController : ControllerBase
{
    private readonly IWeatherForecastQueryService _query;

    public ForecastController(IWeatherForecastQueryService query)
    {
        _query = query;
    }

    /// <summary>
    /// Returns forecasts for the given date and location from multiple providers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WeatherForecastProviderResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<WeatherForecastProviderResultDto>>> Get(
        [FromQuery] DateOnly date,
        [FromQuery] string city,
        [FromQuery] string country,
        CancellationToken cancellationToken)
    {
        var request = new GetWeatherForecastRequestDto(date, city, country);
        var response = await _query.GetForecastAsync(request, cancellationToken);


        return Ok(response);
    }
}
