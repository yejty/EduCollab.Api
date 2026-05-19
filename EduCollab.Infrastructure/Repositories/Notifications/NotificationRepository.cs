using Dapper;
using EduCollab.Application.Repositories.Notifications;
using EduCollab.Application.Services.Notifications;
using EduCollab.Infrastructure.Database;

namespace EduCollab.Infrastructure.Repositories.Notifications
{
    public sealed class NotificationRepository : INotificationRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public NotificationRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<long> InsertPendingAsync(
            NotificationMessage message,
            string? metadataJson,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO Notifications
                    (RecipientEmail, Type, Subject, PlainText, HtmlBody, Status, Attempts, CreatedAtUtc, MetadataJson)
                VALUES
                    (@RecipientEmail, @Type, @Subject, @PlainText, @HtmlBody, @Status, 0, @CreatedAtUtc, CAST(@MetadataJson AS jsonb))
                RETURNING Id;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            return await connection.QuerySingleAsync<long>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        message.RecipientEmail,
                        Type = message.Type.ToString(),
                        message.Content.Subject,
                        message.Content.PlainText,
                        message.Content.HtmlBody,
                        Status = NotificationStatus.Pending.ToString(),
                        CreatedAtUtc = createdAtUtc,
                        MetadataJson = metadataJson
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task MarkSentAsync(long id, DateTimeOffset sentAtUtc, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE Notifications
                SET Status = @Status,
                    Attempts = Attempts + 1,
                    SentAtUtc = @SentAtUtc,
                    LastError = NULL
                WHERE Id = @Id;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { Id = id, Status = NotificationStatus.Sent.ToString(), SentAtUtc = sentAtUtc },
                    cancellationToken: cancellationToken));
        }

        public async Task MarkFailedAsync(long id, string error, DateTimeOffset failedAtUtc, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE Notifications
                SET Status = @Status,
                    Attempts = Attempts + 1,
                    LastError = @LastError
                WHERE Id = @Id;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Id = id,
                        Status = NotificationStatus.Failed.ToString(),
                        LastError = $"[{failedAtUtc:u}] {error}"
                    },
                    cancellationToken: cancellationToken));
        }
    }
}
