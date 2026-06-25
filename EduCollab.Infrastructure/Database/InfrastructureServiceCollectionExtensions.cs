using Dapper;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Content;
using EduCollab.Application.Services.Notifications;
using EduCollab.Infrastructure.Repositories;
using EduCollab.Infrastructure.Services.Notifications;
using EduCollab.Infrastructure.Services.Content;
using EduCollab.Infrastructure.Services.Users;
using EduCollab.Infrastructure.Services.Workspaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduCollab.Infrastructure.Database
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
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
            services.AddScoped<IWorkspaceCreationRequestRepository, WorkspaceCreationRequestRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            return services;
        }
    }
}
