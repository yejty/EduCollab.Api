using EduCollab.Api.Security.AccessToken;
using EduCollab.Api.Tests.Fakes;
using EduCollab.Application.Services.Auth;
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
    public FakeWorkspaceService WorkspaceService { get; } = new();
    public FakeAccessTokenService AccessTokenService { get; } = new();
    public FakeRefreshTokenService RefreshTokenService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserService>();
            services.RemoveAll<IWorkspaceService>();
            services.RemoveAll<IAccessTokenService>();
            services.RemoveAll<IRefreshTokenService>();

            services.AddSingleton<IUserService>(UserService);
            services.AddSingleton<IWorkspaceService>(WorkspaceService);
            services.AddSingleton<IAccessTokenService>(AccessTokenService);
            services.AddSingleton<IRefreshTokenService>(RefreshTokenService);

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
