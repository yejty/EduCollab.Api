using EduCollab.Application.Services.Sessions;
using Microsoft.Extensions.Options;

namespace EduCollab.Api.Services
{
    /// <summary>
    /// Periodically ends Pending/Active sessions idle longer than
    /// <see cref="SessionJoinSettings.AbandonedSessionIdleHours"/>.
    /// </summary>
    public sealed class AbandonedSessionCleanupService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<SessionJoinSettings> _settings;
        private readonly ILogger<AbandonedSessionCleanupService> _logger;

        public AbandonedSessionCleanupService(
            IServiceScopeFactory scopeFactory,
            IOptions<SessionJoinSettings> settings,
            ILogger<AbandonedSessionCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = settings;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sessions = scope.ServiceProvider.GetRequiredService<ISessionService>();
                    var ended = await sessions.EndAbandonedSessionsAsync(stoppingToken);
                    if (ended > 0)
                    {
                        _logger.LogInformation(
                            "Ended {Count} abandoned live session(s) idle longer than {Hours}h.",
                            ended,
                            Math.Max(1, _settings.Value.AbandonedSessionIdleHours));
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Abandoned session cleanup failed.");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
