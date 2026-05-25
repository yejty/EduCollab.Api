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
            await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS Users (Id SERIAL PRIMARY KEY, FirstName VARCHAR(100), LastName VARCHAR(100), Email VARCHAR(255))");
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
                CREATE TABLE IF NOT EXISTS Groups (
                    Id SERIAL PRIMARY KEY,
                    WorkspaceId INT NOT NULL REFERENCES Workspaces(Id) ON DELETE CASCADE,
                    Name VARCHAR(100) NOT NULL,
                    Description VARCHAR(500) NULL,
                    CreatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    UpdatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    CreatedByUserId INT NOT NULL REFERENCES Users(Id) ON DELETE RESTRICT,
                    UserCount INT NOT NULL DEFAULT 1
                );
                """);
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_Groups_WorkspaceId ON Groups (WorkspaceId);");
            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS GroupMembers (
                    GroupId INT NOT NULL REFERENCES Groups(Id) ON DELETE CASCADE,
                    UserId INT NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    Role VARCHAR(50) NOT NULL DEFAULT 'Viewer',
                    JoinedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (GroupId, UserId)
                );
                """);
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_GroupMembers_GroupId ON GroupMembers (GroupId);");
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_GroupMembers_UserId ON GroupMembers (UserId);");
            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS AssetFolders (
                    Id SERIAL PRIMARY KEY,
                    WorkspaceId INT NOT NULL REFERENCES Workspaces(Id) ON DELETE CASCADE,
                    ParentFolderId INT NULL REFERENCES AssetFolders(Id),
                    Name VARCHAR(200) NOT NULL,
                    Path TEXT NOT NULL,
                    CreatedByUserId INT NOT NULL REFERENCES Users(Id) ON DELETE RESTRICT,
                    CreatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    UpdatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );
                """);
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_AssetFolders_WorkspaceId ON AssetFolders (WorkspaceId);");
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_AssetFolders_ParentFolderId ON AssetFolders (ParentFolderId);");
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_AssetFolders_CreatedByUserId ON AssetFolders (CreatedByUserId);");
            await connection.ExecuteAsync(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS UX_AssetFolders_RootName
                ON AssetFolders (WorkspaceId, Name)
                WHERE ParentFolderId IS NULL;
                """);
            await connection.ExecuteAsync(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS UX_AssetFolders_ParentName
                ON AssetFolders (WorkspaceId, ParentFolderId, Name)
                WHERE ParentFolderId IS NOT NULL;
                """);
            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS Assets (
                    Id SERIAL PRIMARY KEY,
                    WorkspaceId INT NOT NULL REFERENCES Workspaces(Id) ON DELETE CASCADE,
                    FolderId INT NULL REFERENCES AssetFolders(Id),
                    OwnerUserId INT NOT NULL REFERENCES Users(Id) ON DELETE RESTRICT,
                    Name VARCHAR(200) NOT NULL,
                    Description TEXT NULL,
                    AssetType VARCHAR(50) NOT NULL,
                    StorageProvider VARCHAR(50) NOT NULL,
                    StorageKey TEXT NOT NULL,
                    MimeType VARCHAR(255) NULL,
                    SizeInBytes BIGINT NULL,
                    CreatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    UpdatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );
                """);
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_Assets_WorkspaceId ON Assets (WorkspaceId);");
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_Assets_FolderId ON Assets (FolderId);");
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_Assets_OwnerUserId ON Assets (OwnerUserId);");
            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS AssetFolderGroupShares (
                    FolderId INT NOT NULL REFERENCES AssetFolders(Id) ON DELETE CASCADE,
                    GroupId INT NOT NULL REFERENCES Groups(Id) ON DELETE CASCADE,
                    Role VARCHAR(20) NOT NULL,
                    CreatedByUserId INT NOT NULL REFERENCES Users(Id) ON DELETE RESTRICT,
                    CreatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (FolderId, GroupId),
                    CONSTRAINT CK_AssetFolderGroupShares_Role
                        CHECK (Role IN ('Viewer', 'Contributor', 'Admin'))
                );
                """);
            await connection.ExecuteAsync(
                "UPDATE AssetFolderGroupShares SET Role = 'Admin' WHERE Role = 'Manager';");
            await connection.ExecuteAsync(
                "ALTER TABLE AssetFolderGroupShares DROP CONSTRAINT IF EXISTS CK_AssetFolderGroupShares_Role;");
            await connection.ExecuteAsync(
                """
                ALTER TABLE AssetFolderGroupShares
                ADD CONSTRAINT CK_AssetFolderGroupShares_Role
                CHECK (Role IN ('Viewer', 'Contributor', 'Admin'));
                """);
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_AssetFolderGroupShares_GroupId ON AssetFolderGroupShares (GroupId);");
            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS AssetGroupShares (
                    AssetId INT NOT NULL REFERENCES Assets(Id) ON DELETE CASCADE,
                    GroupId INT NOT NULL REFERENCES Groups(Id) ON DELETE CASCADE,
                    Role VARCHAR(20) NOT NULL,
                    CreatedByUserId INT NOT NULL REFERENCES Users(Id) ON DELETE RESTRICT,
                    CreatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (AssetId, GroupId),
                    CONSTRAINT CK_AssetGroupShares_Role
                        CHECK (Role IN ('Viewer', 'Contributor', 'Admin'))
                );
                """);
            await connection.ExecuteAsync(
                "UPDATE AssetGroupShares SET Role = 'Admin' WHERE Role = 'Manager';");
            await connection.ExecuteAsync(
                "ALTER TABLE AssetGroupShares DROP CONSTRAINT IF EXISTS CK_AssetGroupShares_Role;");
            await connection.ExecuteAsync(
                """
                ALTER TABLE AssetGroupShares
                ADD CONSTRAINT CK_AssetGroupShares_Role
                CHECK (Role IN ('Viewer', 'Contributor', 'Admin'));
                """);
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_AssetGroupShares_GroupId ON AssetGroupShares (GroupId);");
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
            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS UserLoginCodes (
                    Id BIGSERIAL PRIMARY KEY,
                    UserId INT NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    CodeHash VARCHAR(64) NOT NULL,
                    ExpiresAt TIMESTAMPTZ NOT NULL,
                    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    FailedAttempts INT NOT NULL DEFAULT 0,
                    UsedAt TIMESTAMPTZ NULL);
                """);
            await connection.ExecuteAsync(
                "ALTER TABLE UserLoginCodes ADD COLUMN IF NOT EXISTS FailedAttempts INT NOT NULL DEFAULT 0;");
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_UserLoginCodes_UserId ON UserLoginCodes (UserId);");
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_UserLoginCodes_CodeHash ON UserLoginCodes (CodeHash);");
            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS WorkspaceInvitations (
                    Id BIGSERIAL PRIMARY KEY,
                    WorkspaceId INT NOT NULL REFERENCES Workspaces(Id) ON DELETE CASCADE,
                    Email VARCHAR(255) NOT NULL,
                    TokenHash VARCHAR(64) NOT NULL UNIQUE,
                    ExpiresAt TIMESTAMPTZ NOT NULL,
                    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    UsedAt TIMESTAMPTZ NULL,
                    InvitedByUserId INT NULL REFERENCES Users(Id) ON DELETE SET NULL);
                """);
            await connection.ExecuteAsync(
                """
                ALTER TABLE WorkspaceInvitations
                ADD COLUMN IF NOT EXISTS InvitedByUserId INT NULL REFERENCES Users(Id) ON DELETE SET NULL;
                """);
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_WorkspaceInvitations_WorkspaceId ON WorkspaceInvitations (WorkspaceId);");
            await connection.ExecuteAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_WorkspaceInvitations_TokenHash ON WorkspaceInvitations (TokenHash);");
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_WorkspaceInvitations_Email ON WorkspaceInvitations (Email);");
            await connection.ExecuteAsync("ALTER TABLE Users ADD COLUMN IF NOT EXISTS EmailConfirmedAtUtc TIMESTAMPTZ NULL;");
            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS UserEmailConfirmationTokens (
                    Id BIGSERIAL PRIMARY KEY,
                    UserId INT NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    TokenHash VARCHAR(64) NOT NULL UNIQUE,
                    ExpiresAt TIMESTAMPTZ NOT NULL,
                    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    UsedAt TIMESTAMPTZ NULL);
                """);
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_UserEmailConfirmationTokens_UserId ON UserEmailConfirmationTokens (UserId);");

            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS Notifications (
                    Id BIGSERIAL PRIMARY KEY,
                    RecipientEmail VARCHAR(255) NOT NULL,
                    Type VARCHAR(100) NOT NULL,
                    Subject TEXT NOT NULL,
                    PlainText TEXT NOT NULL,
                    HtmlBody TEXT NULL,
                    Status VARCHAR(50) NOT NULL,
                    Attempts INT NOT NULL DEFAULT 0,
                    CreatedAtUtc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    SentAtUtc TIMESTAMPTZ NULL,
                    LastError TEXT NULL,
                    MetadataJson JSONB NULL
                );
                """);
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_Notifications_RecipientEmail ON Notifications (RecipientEmail);");
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_Notifications_Status ON Notifications (Status);");
            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS IX_Notifications_Type ON Notifications (Type);");

        }
    }
}
