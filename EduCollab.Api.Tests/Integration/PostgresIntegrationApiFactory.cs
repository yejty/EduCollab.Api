using EduCollab.Application.Services.Notifications;
using EduCollab.Api.Tests.Integration;
using EduCollab.Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace EduCollab.Api.Tests;

public sealed class PostgresIntegrationApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private const string DefaultAdminConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres;Pooling=false";

    private readonly string _databaseName;
    private readonly string _adminConnectionString;
    private readonly string _databaseConnectionString;
    private bool _initialized;

    public SpyEmailSender EmailSender { get; } = new();

    public PostgresIntegrationApiFactory()
    {
        _databaseName = $"educollab_test_{Guid.NewGuid():N}";

        var adminBuilder = new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("EDUCOLLAB_TEST_POSTGRES_CONNECTION")
            ?? DefaultAdminConnectionString);

        _adminConnectionString = adminBuilder.ConnectionString;

        var databaseBuilder = new NpgsqlConnectionStringBuilder(adminBuilder.ConnectionString)
        {
            Database = _databaseName,
        };

        _databaseConnectionString = databaseBuilder.ConnectionString;
    }

    public static async Task<PostgresIntegrationApiFactory> CreateInitializedAsync()
    {
        var factory = new PostgresIntegrationApiFactory();
        await factory.InitializeAsync();
        return factory;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = _databaseConnectionString,
                ["Email:Enabled"] = "false",
                ["EmailConfirmation:FrontendConfirmUrl"] = string.Empty,
                ["Jwt:Issuer"] = "EduCollab.Api.Tests",
                ["Jwt:Audience"] = "EduCollab.Api.Tests.Client",
                ["Jwt:SecretKey"] = "integration-tests-secret-key-should-be-long-enough-12345",
                ["Jwt:ExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "14",
                ["PasswordReset:TokenExpirationMinutes"] = "3",
                ["EmailConfirmation:TokenExpirationHours"] = "24",
                ["LoginCode:CodeExpirationMinutes"] = "3",
                ["LoginCode:MaxAttempts"] = "3",
                ["WorkspaceInvitation:TokenExpirationHours"] = "168",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DatabaseOptions>();
            services.RemoveAll<IDbConnectionFactory>();
            services.RemoveAll<DbInitializer>();
            services.RemoveAll<IEmailSender>();

            services.AddSingleton(new DatabaseOptions
            {
                ConnectionString = _databaseConnectionString,
            });
            services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlConnectionFactory(_databaseConnectionString));
            services.AddSingleton<DbInitializer>();
            services.AddSingleton<IEmailSender>(EmailSender);
        });
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        await CreateDatabaseAsync();

        using var _ = CreateClient();
        var dbInitializer = Services.GetRequiredService<DbInitializer>();
        await dbInitializer.InitializeAsync();

        EmailSender.Clear();
        _initialized = true;
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await DropDatabaseAsync();
        }
        finally
        {
            Dispose();
        }
    }

    private async Task CreateDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{_databaseName}\";";
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();

        await using (var terminateCommand = connection.CreateCommand())
        {
            terminateCommand.CommandText =
                """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @databaseName
                  AND pid <> pg_backend_pid();
                """;
            terminateCommand.Parameters.AddWithValue("databaseName", _databaseName);
            await terminateCommand.ExecuteNonQueryAsync();
        }

        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\";";
        await dropCommand.ExecuteNonQueryAsync();
    }
}
