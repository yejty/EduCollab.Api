using Dapper;
using EduCollab.Application.Repositories.RefreshToken;
using EduCollab.Application.Repositories.Users;
using EduCollab.Application.Services.Notifications;
using EduCollab.Infrastructure.Repositories.RefreshToken;
using EduCollab.Infrastructure.Repositories.Users;
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
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            return services;
        }
    }
}
