using Microsoft.Extensions.DependencyInjection;

namespace EduCollab.Application.Database
{
    public static class DatabaseServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, DatabaseOptions options)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(options.ConnectionString);

            services.AddSingleton(options);
            services.AddSingleton<IDbConnectionFactory>(_ => new NpqsqlConnectionFactory(options.ConnectionString));
            services.AddSingleton<DbInitializer>();

            return services;
        }
    }
}
