using EduCollab.Api.Security;
using EduCollab.Api.Tests.Fakes;
using EduCollab.Application.Services.Auth;
using EduCollab.Application.Services.Scenes;
using EduCollab.Application.Services.Users;
using EduCollab.Application.Services.Workspaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EduCollab.Api.Tests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeUserService UserService { get; } = new();
    public FakeUserPreferencesService UserPreferencesService { get; } = new();
    public FakeWorkspaceService WorkspaceService { get; } = new();
    public FakeWorkspaceThumbnailService WorkspaceThumbnailService { get; } = new();
    public FakeWorkspaceCreationRequestService WorkspaceCreationRequestService { get; } = new();
    public FakeAccessTokenService AccessTokenService { get; } = new();
    public FakeRefreshTokenService RefreshTokenService { get; } = new();
    public FakeSceneService SceneService { get; } = new();

    public FakePlatformAdminAuthorization PlatformAdminAuthorization =>
        Services.GetRequiredService<FakePlatformAdminAuthorization>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserService>();
            services.RemoveAll<IUserPreferencesService>();
            services.RemoveAll<IWorkspaceService>();
            services.RemoveAll<IWorkspaceThumbnailService>();
            services.RemoveAll<IWorkspaceCreationRequestService>();
            services.RemoveAll<IAccessTokenService>();
            services.RemoveAll<IRefreshTokenService>();
            services.RemoveAll<ISceneService>();
            services.RemoveAll<IPlatformAdminAuthorization>();

            services.AddSingleton<IUserService>(UserService);
            services.AddSingleton<IUserPreferencesService>(UserPreferencesService);
            services.AddSingleton<IWorkspaceService>(WorkspaceService);
            services.AddSingleton<IWorkspaceThumbnailService>(WorkspaceThumbnailService);
            services.AddSingleton<IWorkspaceCreationRequestService>(WorkspaceCreationRequestService);
            services.AddSingleton<IAccessTokenService>(AccessTokenService);
            services.AddSingleton<IRefreshTokenService>(RefreshTokenService);
            services.AddSingleton<ISceneService>(SceneService);
            services.AddSingleton<FakePlatformAdminAuthorization>();
            services.AddSingleton<IPlatformAdminAuthorization>(sp => sp.GetRequiredService<FakePlatformAdminAuthorization>());

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName,
                _ => { });
        });
    }
}
