using Dapper;

namespace EduCollab.Infrastructure.Database
{
    public class DbInitializer
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public DbInitializer(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task InitializeAsync()
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS Users (Id SERIAL PRIMARY KEY, FirstName VARCHAR(100), LastName VARCHAR(100), Email VARCHAR(255));");
            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS Workspaces (
                    Id SERIAL PRIMARY KEY,
                    Name VARCHAR(200) NOT NULL,
                    Description TEXT NULL,
                    CreatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    UpdatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    CreatedByUserId INT NULL REFERENCES Users(Id) ON DELETE SET NULL,
                    IsArchived BOOLEAN NOT NULL DEFAULT FALSE
                );
                """);
            await connection.ExecuteAsync("ALTER TABLE Workspaces ADD COLUMN IF NOT EXISTS Description TEXT NULL;");
            await connection.ExecuteAsync("ALTER TABLE Workspaces ADD COLUMN IF NOT EXISTS CreatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW();");
            await connection.ExecuteAsync("ALTER TABLE Workspaces ADD COLUMN IF NOT EXISTS UpdatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW();");
            await connection.ExecuteAsync(
                """
                ALTER TABLE Workspaces
                ADD COLUMN IF NOT EXISTS CreatedByUserId INT NULL
                REFERENCES Users(Id) ON DELETE SET NULL;
                """);
            await connection.ExecuteAsync("ALTER TABLE Workspaces ADD COLUMN IF NOT EXISTS IsArchived BOOLEAN NOT NULL DEFAULT FALSE;");
            await connection.ExecuteAsync(
                """
                ALTER TABLE Users
                ADD COLUMN IF NOT EXISTS WorkspaceId INT NULL
                REFERENCES Workspaces(Id) ON DELETE SET NULL;
                """);
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_Users_WorkspaceId ON Users (WorkspaceId) WHERE WorkspaceId IS NOT NULL;");
            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS WorkspaceMembers (
                    WorkspaceId INT NOT NULL REFERENCES Workspaces(Id) ON DELETE CASCADE,
                    UserId INT NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    Role VARCHAR(50) NOT NULL DEFAULT 'Member',
                    JoinedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (WorkspaceId, UserId)
                );
                """);
            await connection.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_WorkspaceMembers_UserId ON WorkspaceMembers (UserId);");
            await connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_WorkspaceMembers_WorkspaceId ON WorkspaceMembers (WorkspaceId);");
            await connection.ExecuteAsync(
                """
                INSERT INTO WorkspaceMembers (WorkspaceId, UserId, Role, JoinedAtUtc)
                SELECT WorkspaceId, Id, 'Member', NOW()
                FROM Users
                WHERE WorkspaceId IS NOT NULL
                ON CONFLICT (WorkspaceId, UserId) DO NOTHING;
                """);
            await connection.ExecuteAsync("ALTER TABLE Users ADD COLUMN IF NOT EXISTS PasswordHash TEXT;");
            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS UserRefreshTokens (
                    Id BIGSERIAL PRIMARY KEY,
                    UserId INT NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    TokenHash VARCHAR(64) NOT NULL UNIQUE,
                    ExpiresAt TIMESTAMPTZ NOT NULL,
                    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    RevokedAt TIMESTAMPTZ NULL);
                """);
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_UserRefreshTokens_UserId ON UserRefreshTokens (UserId);");

            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS UserPasswordResetTokens (
                    Id BIGSERIAL PRIMARY KEY,
                    UserId INT NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    TokenHash VARCHAR(64) NOT NULL,
                    ExpiresAt TIMESTAMPTZ NOT NULL,
                    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    UsedAt TIMESTAMPTZ NULL);
                """);
            await connection.ExecuteAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_UserPasswordResetTokens_TokenHash ON UserPasswordResetTokens (TokenHash);");
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_UserPasswordResetTokens_UserId ON UserPasswordResetTokens (UserId);");
        }
    }
}
