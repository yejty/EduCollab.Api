using EduCollab.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EduCollab.Api.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public sealed class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        /// <summary>
        /// Returns API and dependency health status. Does not require authentication.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">All health checks passed.</response>
        /// <response code="503">One or more health checks failed.</response>
        [HttpGet(ApiEndpoints.Health.Get)]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
        {
            var report = await _healthCheckService.CheckHealthAsync(cancellationToken);

            var response = new HealthResponse
            {
                Status = report.Status.ToString(),
                Checks = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value.Status.ToString(),
                    StringComparer.Ordinal),
            };

            return report.Status == HealthStatus.Healthy
                ? Ok(response)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
    }
}
