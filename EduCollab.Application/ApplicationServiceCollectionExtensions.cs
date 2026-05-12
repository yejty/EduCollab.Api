using EduCollab.Application.Repositories.RefreshToken;
using EduCollab.Application.Repositories.Users;
using EduCollab.Application.Services.Auth;
using EduCollab.Application.Services.Notifications;
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
            services.Configure<RefreshTokenSettings>(configuration.GetSection("Jwt"));
            services.Configure<PasswordResetSettings>(configuration.GetSection(PasswordResetSettings.SectionName));
            services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
            services.AddSingleton<IPasswordHasher<PasswordHasherUser>, PasswordHasher<PasswordHasherUser>>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IWorkspaceService, WorkspaceService>();
            return services;
        }
    }
}
