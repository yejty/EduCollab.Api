using Space.Application.Services.Auth;
using Space.Application.Services.Assets;
using Space.Application.Services.Content;
using Space.Application.Services.Flows;
using Space.Application.Services.Groups;
using Space.Application.Services.Notifications;
using Space.Application.Services.Scenes;
using Space.Application.Services.Sessions;
using Space.Application.Services.Users;
using Space.Application.Services.Workspaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Space.Application
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
            services.Configure<PlatformAdminOptions>(configuration.GetSection(PlatformAdminOptions.SectionName));
            services.Configure<WorkspaceContentStorageOptions>(configuration.GetSection(WorkspaceContentStorageOptions.SectionName));
            services.Configure<WorkspaceCreationApprovalSettings>(configuration.GetSection(WorkspaceCreationApprovalSettings.SectionName));
            services.Configure<SessionJoinSettings>(configuration.GetSection(SessionJoinSettings.SectionName));
            services.Configure<InternalApiSettings>(configuration.GetSection(InternalApiSettings.SectionName));
            services.AddSingleton<IPasswordHasher<PasswordHasherUser>, PasswordHasher<PasswordHasherUser>>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserPreferencesService, UserPreferencesService>();
            services.AddScoped<IPlatformAdminAuthorization, PlatformAdminAuthorization>();
            services.AddScoped<IWorkspaceService, WorkspaceService>();
            services.AddScoped<IWorkspaceThumbnailService, WorkspaceThumbnailService>();
            services.AddScoped<IWorkspaceCreationRequestService, WorkspaceCreationRequestService>();
            services.AddScoped<IGroupAccessResolver, GroupAccessResolver>();
            services.AddScoped<IGroupService, GroupService>();
            services.AddScoped<IAssetService, AssetService>();
            services.AddScoped<ISceneService, SceneService>();
            services.AddScoped<IFlowService, FlowService>();
            services.AddScoped<ISessionService, SessionService>();
            return services;
        }
    }
}
