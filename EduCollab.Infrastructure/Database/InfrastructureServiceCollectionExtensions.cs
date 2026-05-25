using Dapper;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Notifications;
using EduCollab.Infrastructure.Repositories;
using EduCollab.Infrastructure.Services.Notifications;
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

            services.AddSingleton<IEmailSender, SmtpEmailSender>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IAssetFolderRepository, AssetFolderRepository>();
            services.AddScoped<IAssetRepository, AssetRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            return services;
        }
    }
}
