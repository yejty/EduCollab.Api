using Space.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hub.Api.MinimalApis;

internal static class HealthEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(ApiEndpoints.Health.Get, Get)
            .AsApi("Health_Get", "Health")
            .AllowAnonymous();
    }

    /// <summary>
    /// Returns API and dependency health status. Does not require authentication.
    /// </summary>
    /// <response code="200">All health checks passed.</response>
    /// <response code="503">One or more health checks failed.</response>
    [AllowAnonymous]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public static async Task<IResult> Get(HealthCheckService healthCheckService, CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);
        var response = new HealthResponse
        {
            Status = report.Status.ToString(),
            Checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Status.ToString(),
                StringComparer.Ordinal),
        };

        return report.Status == HealthStatus.Healthy
            ? ApiResults.Ok(response)
            : ApiResults.Status(StatusCodes.Status503ServiceUnavailable, response);
    }
}
