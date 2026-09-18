using Xunit;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Sample.Api;

namespace Sample.Api.IntegrationTests;

/*
 * WebApplicationFactory is used because these are API integration tests.
 * It starts the real ASP.NET Core application pipeline in memory, so routing,
 * model binding and JSON serialization are covered without starting a separate
 * server or depending on a network port. This keeps the tests fast and stable
 * enough to run on every pull request.
 *
 * Alternatives:
 * - Calling WeatherForecastController directly would be simpler and faster,
 *   but that would be a unit test and would not cover the HTTP pipeline.
 * - TestServer could be configured directly, but WebApplicationFactory gives
 *   the same type of in-memory integration testing with less setup.
 * - Starting Kestrel on a real port would be closer to a deployed environment,
 *   but adds unnecessary setup and potential CI instability for this API.
 */
public class WeatherForecastControllerTests
    : IClassFixture<WebApplicationFactory<Sample.Api.Program>>
{
    private static readonly string[] ExpectedSummaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild",
        "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private readonly HttpClient _client;

    public WeatherForecastControllerTests(
        WebApplicationFactory<Sample.Api.Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task Get_WithValidDate_ReturnsFiveForecastsForFollowingDays()
    {
        var response = await _client.GetAsync(
            "/WeatherForecast?date=2026-09-16");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        var forecasts =
            await response.Content.ReadFromJsonAsync<WeatherForecast[]>();

        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);

        var requestedDate = new DateOnly(2026, 9, 16);

        for (var i = 0; i < forecasts.Length; i++)
        {
            Assert.Equal(requestedDate.AddDays(i + 1), forecasts[i].Date);
            Assert.InRange(forecasts[i].TemperatureC, -20, 54);
            Assert.Contains(forecasts[i].Summary!, ExpectedSummaries);
        }
    }

    [Fact]
    public async Task Get_WithSameDate_ReturnsSameForecast()
    {
        var firstResponse = await _client.GetFromJsonAsync<WeatherForecast[]>(
            "/WeatherForecast?date=2026-09-16");

        var secondResponse = await _client.GetFromJsonAsync<WeatherForecast[]>(
            "/WeatherForecast?date=2026-09-16");

        Assert.NotNull(firstResponse);
        Assert.NotNull(secondResponse);
        Assert.Equal(firstResponse, secondResponse);
    }

    [Fact]
    public async Task Get_WithoutDate_ReturnsFiveForecasts()
    {
        var response = await _client.GetAsync("/WeatherForecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecasts =
            await response.Content.ReadFromJsonAsync<WeatherForecast[]>();

        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);
    }

    [Fact]
    public async Task Get_WithInvalidDate_ReturnsBadRequest()
    {
        var response = await _client.GetAsync(
            "/WeatherForecast?date=not-a-date");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
