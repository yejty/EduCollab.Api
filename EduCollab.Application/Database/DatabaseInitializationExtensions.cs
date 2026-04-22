using Microsoft.Extensions.DependencyInjection;

namespace EduCollab.Application.Database
{
    public static class DatabaseInitializationExtensions
    {
        public static async Task InitializeDatabaseAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var dbInitializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();

            await dbInitializer.InitializeAsync();
        }
    }
}
