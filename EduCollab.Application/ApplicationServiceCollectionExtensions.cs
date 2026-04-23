using EduCollab.Application.Services.Users;
using EduCollab.Application.Services.Workspaces;
using Microsoft.Extensions.DependencyInjection;

namespace EduCollab.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IWorkspaceService, WorkspaceService>();
            return services;
        }
    }
}
