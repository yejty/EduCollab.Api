using System.Net;
using System.Net.Http.Json;
using EduCollab.Application.Exceptions;
using EduCollab.Application.Models.Users;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Users;

namespace EduCollab.Api.Tests;

public sealed class UsersControllerEndpointTests
{
    [Fact]
    public async Task Register_ReturnsCreated_WhenRegistrationSucceeds()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.UserService.RegisterAsyncHandler = (user, _, _) =>
        {
            user.Id = 101;
            return Task.FromResult(true);
        };

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/register", new RegisterUserRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            Password = "Pass123!",
            ConfirmPassword = "Pass123!",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.ReadAsJsonAsync<RegistrationSubmittedResponse>();
        Assert.Contains("Check your email", body.Message);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenPasswordsDoNotMatch()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/register", new RegisterUserRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            Password = "Pass123!",
            ConfirmPassword = "Mismatch123!",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ErrorResponse>();
        Assert.Equal("password_mismatch", body.Error);
    }

    [Fact]
    public async Task ConfirmEmail_ReturnsTokens_WhenUserIsConfirmed()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.UserService.ConfirmEmailAsyncHandler = (_, _, _) => Task.FromResult<User?>(new User
        {
            Id = 12,
            Email = "jane@example.com",
        });
        factory.AccessTokenService.CreateHandler = (userId, _) => $"access-{userId}";
        factory.RefreshTokenService.CreateAsyncHandler = (userId, _) => Task.FromResult($"refresh-{userId}");

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/registration-confirm", new ConfirmEmailRequest
        {
            Email = "jane@example.com",
            Token = "confirm-token",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<TokensResponse>();
        Assert.Equal("access-12", body.AccessToken);
        Assert.Equal("refresh-12", body.RefreshToken);
    }

    [Fact]
    public async Task LoginWithCode_ReturnsUnauthorized_WhenCodeIsLocked()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.UserService.LoginWithCodeAsyncHandler = (_, _, _) => Task.FromResult(new LoginWithCodeResult
        {
            IsLocked = true,
        });

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/login/confirm-code", new ConfirmLoginCodeRequest
        {
            Email = "jane@example.com",
            Code = "123456",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ErrorResponse>();
        Assert.Equal("login_code_locked", body.Error);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.UserService.LoginAsyncHandler = (_, _, _) => Task.FromResult<User?>(null);

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/login", new LoginRequest
        {
            Email = "jane@example.com",
            Password = "wrong",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ErrorResponse>();
        Assert.Equal("invalid_login", body.Error);
    }

    [Fact]
    public async Task RefreshToken_ReturnsTokens_WhenSessionExists()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.RefreshTokenService.RefreshAsyncHandler = (_, _) => Task.FromResult<RefreshSessionResult?>(new RefreshSessionResult
        {
            User = new User
            {
                Id = 13,
                Email = "jane@example.com",
            },
            RefreshToken = "new-refresh-token",
        });
        factory.AccessTokenService.CreateHandler = (userId, _) => $"access-{userId}";

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/token", new RefreshTokenRequest
        {
            RefreshToken = "old-refresh-token",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<TokensResponse>();
        Assert.Equal("access-13", body.AccessToken);
        Assert.Equal("new-refresh-token", body.RefreshToken);
    }

    [Fact]
    public async Task GetCurrentUser_RequiresAuthentication()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsUser_WhenAuthenticated()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.UserService.GetCurrentUserAsyncHandler = _ => Task.FromResult<User?>(new User
        {
            Id = 21,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            WorkspaceId = 7,
        });

        using var client = factory.CreateClient(userId: 21, email: "jane@example.com");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsJsonAsync<UserResponse>();
        Assert.Equal(21L, body.Id);
        Assert.Equal(7, body.WorkspaceId);
    }

    [Fact]
    public async Task Update_ReturnsForbidden_WhenUserServiceThrowsAccessDenied()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.UserService.UpdateUserByIdAsyncHandler = (_, _) => throw new AccessDeniedException("No access.");

        using var client = factory.CreateClient(userId: 22);

        var response = await client.PutAsJsonAsync("/api/users/99", new UpdateUserProfileRequest
        {
            FirstName = "New",
            LastName = "Name",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ErrorResponse>();
        Assert.Equal("forbidden", body.Error);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenUserIsDeleted()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.UserService.DeleteUserByIdAsyncHandler = (_, _) => Task.FromResult(true);

        using var client = factory.CreateClient(userId: 23);

        var response = await client.DeleteAsync("/api/users/23");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ResetPasswordConfirm_ReturnsOk_WhenRequestIsAccepted()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/reset-password-confirm", new ConfirmPasswordResetRequest
        {
            Email = "jane@example.com",
            Token = "reset-token",
            NewPassword = "NewPass123!",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ReturnsOk_WhenAuthenticated()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient(userId: 24);

        var response = await client.PostAsJsonAsync("/api/users/change-password", new ChangePasswordRequest
        {
            Password = "OldPass123!",
            NewPassword = "NewPass123!",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
