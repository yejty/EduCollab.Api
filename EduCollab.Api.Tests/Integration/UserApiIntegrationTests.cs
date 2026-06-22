using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Responses;
using EduCollab.Contracts.Responses.Users;

namespace EduCollab.Api.Tests.Integration;

public sealed class UserApiIntegrationTests
{
    [Fact]
    public async Task Register_Confirm_Login_Refresh_And_ProfileFlow_Works()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var client = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "Pass123!";

        var registerResponse = await client.PostAsJsonAsync("/api/users/register", new RegisterUserRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = email,
            Password = password,
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginBeforeConfirmResponse = await client.PostAsJsonAsync("/api/users/login", new LoginRequest
        {
            Email = email,
            Password = password,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginBeforeConfirmResponse.StatusCode);

        var confirmToken = factory.GetConfirmationToken(email);

        var confirmResponse = await client.PostAsJsonAsync("/api/users/registration-confirm", new ConfirmEmailRequest
        {
            Email = email,
            Token = confirmToken,
        });

        confirmResponse.EnsureSuccessStatusCode();
        var tokens = await confirmResponse.ReadAsJsonAsync<TokensResponse>();

        client.SetBearerToken(tokens.AccessToken);

        var meResponse = await client.GetAsync("/api/users/me");
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.ReadAsJsonAsync<UserResponse>();

        Assert.Equal(email, me.Email);
        Assert.True(me.Id > 0);

        var refreshResponse = await client.PostAsJsonAsync("/api/users/token", new RefreshTokenRequest
        {
            RefreshToken = tokens.RefreshToken,
        });

        refreshResponse.EnsureSuccessStatusCode();
        var refreshed = await refreshResponse.ReadAsJsonAsync<TokensResponse>();
        Assert.False(string.IsNullOrWhiteSpace(refreshed.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshed.RefreshToken));

        client.SetBearerToken(refreshed.AccessToken);

        var updateResponse = await client.PutAsJsonAsync($"/api/users/{me.Id}", new UpdateUserProfileRequest
        {
            FirstName = "Janet",
            LastName = "Updated",
        });

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.ReadAsJsonAsync<UserResponse>();
        Assert.Equal("Janet", updated.FirstName);
        Assert.Equal("Updated", updated.LastName);

        var deleteResponse = await client.DeleteAsync($"/api/users/{me.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task PasswordReset_And_LoginCode_Flows_Work()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var client = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "Pass123!";
        const string newPassword = "Pass456!";

        await client.RegisterAndConfirmAsync(factory, "John", "Smith", email, password);

        factory.EmailSender.Clear();

        var requestCodeResponse = await client.PostAsJsonAsync("/api/users/login/request-code", new RequestLoginCodeRequest
        {
            Email = email,
        });

        Assert.Equal(HttpStatusCode.OK, requestCodeResponse.StatusCode);

        var code = factory.GetLoginCode(email);

        var invalidCodeResponse = await client.PostAsJsonAsync("/api/users/login/confirm-code", new ConfirmLoginCodeRequest
        {
            Email = email,
            Code = "000000",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, invalidCodeResponse.StatusCode);
        var invalidCode = await invalidCodeResponse.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("invalid_login_code", invalidCode.Error);

        var validCodeResponse = await client.PostAsJsonAsync("/api/users/login/confirm-code", new ConfirmLoginCodeRequest
        {
            Email = email,
            Code = code,
        });

        validCodeResponse.EnsureSuccessStatusCode();

        factory.EmailSender.Clear();

        var resetRequestResponse = await client.PostAsJsonAsync("/api/users/reset-password", new PasswordResetRequest
        {
            Email = email,
        });

        Assert.Equal(HttpStatusCode.OK, resetRequestResponse.StatusCode);

        var resetToken = factory.GetPasswordResetToken(email);

        var confirmResetResponse = await client.PostAsJsonAsync("/api/users/reset-password-confirm", new ConfirmPasswordResetRequest
        {
            Email = email,
            Token = resetToken,
            NewPassword = newPassword,
        });

        Assert.Equal(HttpStatusCode.OK, confirmResetResponse.StatusCode);

        var oldPasswordLogin = await client.PostAsJsonAsync("/api/users/login", new LoginRequest
        {
            Email = email,
            Password = password,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);

        var newPasswordLogin = await client.LoginAsync(email, newPassword);
        Assert.False(string.IsNullOrWhiteSpace(newPasswordLogin.AccessToken));
    }

    [Fact]
    public async Task Login_ReturnsUserNotFound_WhenEmailDoesNotExist()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/login", new LoginRequest
        {
            Email = $"missing-{Guid.NewGuid():N}@example.com",
            Password = "Pass123!",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("user_not_found", body.Error);
    }

    [Fact]
    public async Task RequestLoginCode_ReturnsUserNotFound_WhenEmailDoesNotExist()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/login/request-code", new RequestLoginCodeRequest
        {
            Email = $"missing-{Guid.NewGuid():N}@example.com",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.ReadAsJsonAsync<ApiProblemDetailsTestResponse>();
        Assert.Equal("user_not_found", body.Error);
    }

    [Fact]
    public async Task ResendEmailConfirmation_ReplacesOldToken_And_AllowsConfirmation()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var client = factory.CreateClient();

        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "Pass123!";

        var registerResponse = await client.PostAsJsonAsync("/api/users/register", new RegisterUserRequest
        {
            FirstName = "Resend",
            LastName = "User",
            Email = email,
            Password = password,
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var firstToken = factory.GetConfirmationToken(email);

        factory.EmailSender.Clear();

        var resendResponse = await client.PostAsJsonAsync("/api/users/registration-confirm/resend", new ResendEmailConfirmationRequest
        {
            Email = email,
        });

        Assert.Equal(HttpStatusCode.OK, resendResponse.StatusCode);
        var resentToken = factory.GetConfirmationToken(email);

        Assert.NotEqual(firstToken, resentToken);

        var oldConfirmResponse = await client.PostAsJsonAsync("/api/users/registration-confirm", new ConfirmEmailRequest
        {
            Email = email,
            Token = firstToken,
        });

        Assert.Equal(HttpStatusCode.BadRequest, oldConfirmResponse.StatusCode);

        var newConfirmResponse = await client.PostAsJsonAsync("/api/users/registration-confirm", new ConfirmEmailRequest
        {
            Email = email,
            Token = resentToken,
        });

        newConfirmResponse.EnsureSuccessStatusCode();
        var tokens = await newConfirmResponse.ReadAsJsonAsync<TokensResponse>();
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
    }
}
