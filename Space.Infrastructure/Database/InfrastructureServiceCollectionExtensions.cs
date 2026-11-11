using Dapper;
using Space.Application.Repositories;
using Space.Application.Services.Content;
using Space.Application.Services.Notifications;
using Space.Infrastructure.Repositories;
using Space.Infrastructure.Services.Notifications;
using Space.Infrastructure.Services.Content;
using Space.Infrastructure.Services.Users;
using Space.Infrastructure.Services.Workspaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Space.Infrastructure.Database
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            SqlMapper.AddTypeHandler(new WorkspaceRoleTypeHandler());

            var databaseOptions = configuration
                .GetSection(DatabaseOptions.SectionName)
                .Get<DatabaseOptions>()
                ?? throw new InvalidOperationException($"Configuration section '{DatabaseOptions.SectionName}' is missing.");

            services.AddSingleton(databaseOptions);
            services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlConnectionFactory(databaseOptions.ConnectionString));
            services.AddSingleton<DbInitializer>();

            services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
            services.Configure<UserPreferencesStorageOptions>(configuration.GetSection(UserPreferencesStorageOptions.SectionName));
            services.Configure<WorkspaceThumbnailStorageOptions>(configuration.GetSection(WorkspaceThumbnailStorageOptions.SectionName));
            services.Configure<WorkspaceContentStorageOptions>(configuration.GetSection(WorkspaceContentStorageOptions.SectionName));

            services.AddSingleton<IEmailSender, SmtpEmailSender>();
            services.AddSingleton<IUserPreferencesStore, FileUserPreferencesStore>();
            services.AddSingleton<IWorkspaceThumbnailStore, FileWorkspaceThumbnailStore>();
            services.AddSingleton<IAssetContentStore, FileAssetContentStore>();
            services.AddSingleton<ISceneContentStore, FileSceneContentStore>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IAssetRepository, AssetRepository>();
            services.AddScoped<ISceneRepository, SceneRepository>();
            services.AddScoped<IFlowRepository, FlowRepository>();
            services.AddScoped<ISessionRepository, SessionRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
            services.AddScoped<IWorkspaceCreationRequestRepository, WorkspaceCreationRequestRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            return services;
        }
    }
}
