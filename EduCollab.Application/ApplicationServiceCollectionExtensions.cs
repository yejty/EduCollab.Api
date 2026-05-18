using EduCollab.Application.Services.Auth;
using EduCollab.Application.Services.Users;
using EduCollab.Application.Services.Workspaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduCollab.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RefreshTokenSettings>(configuration.GetSection(RefreshTokenSettings.SectionName));
            services.Configure<PasswordResetSettings>(configuration.GetSection(PasswordResetSettings.SectionName));
            services.Configure<EmailConfirmationSettings>(configuration.GetSection(EmailConfirmationSettings.SectionName));
            services.Configure<LoginCodeSettings>(configuration.GetSection(LoginCodeSettings.SectionName));
            services.Configure<WorkspaceInvitationSettings>(configuration.GetSection(WorkspaceInvitationSettings.SectionName));
            services.AddSingleton<IPasswordHasher<PasswordHasherUser>, PasswordHasher<PasswordHasherUser>>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IWorkspaceService, WorkspaceService>();
            return services;
        }
    }
}
