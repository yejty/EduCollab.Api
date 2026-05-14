using EduCollab.Infrastructure.Database;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EduCollab.Api.Health
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        public const string Name = "Database";
        private readonly IDbConnectionFactory _dbconnectionFactory;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(IDbConnectionFactory dbconnectionFactory, ILogger<DatabaseHealthCheck> logger)
        {
            _dbconnectionFactory = dbconnectionFactory;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
        {
            try
            {
                _ = await _dbconnectionFactory.CreateConnectionAsync();
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                const string errorMessage = "Database is unhealthy";
                _logger.LogError(ex, errorMessage);
                return HealthCheckResult.Unhealthy(errorMessage, ex);
            }
        }
    }
}
