using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Responses.Users;

namespace EduCollab.Api.Tests.Integration;

internal static partial class IntegrationTestHelpers
{
    public static void SetBearerToken(this HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public static async Task<TokensResponse> RegisterAndConfirmAsync(
        this HttpClient client,
        PostgresIntegrationApiFactory factory,
        string firstName,
        string lastName,
        string email,
        string password)
    {
        factory.EmailSender.Clear();

        var registerResponse = await client.PostAsJsonAsync("/api/users/register", new RegisterUserRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Password = password,
            ConfirmPassword = password,
        });

        registerResponse.EnsureSuccessStatusCode();

        var token = ExtractMatch(
            factory.EmailSender.GetLatest(email, "Confirm your EduCollab email").Content.PlainText,
            ConfirmationTokenRegex(),
            "email confirmation token");

        var confirmResponse = await client.PostAsJsonAsync("/api/users/registration-confirm", new ConfirmEmailRequest
        {
            Email = email,
            Token = token,
        });

        confirmResponse.EnsureSuccessStatusCode();
        return await confirmResponse.ReadAsJsonAsync<TokensResponse>();
    }

    public static async Task<TokensResponse> LoginAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/users/login", new LoginRequest
        {
            Email = email,
            Password = password,
        });

        response.EnsureSuccessStatusCode();
        return await response.ReadAsJsonAsync<TokensResponse>();
    }

    public static string GetPasswordResetToken(this PostgresIntegrationApiFactory factory, string email) =>
        ExtractMatch(
            factory.EmailSender.GetLatest(email, "Reset your EduCollab password").Content.PlainText,
            PasswordResetTokenRegex(),
            "password reset token");

    public static string GetConfirmationToken(this PostgresIntegrationApiFactory factory, string email) =>
        ExtractMatch(
            factory.EmailSender.GetLatest(email, "Confirm your EduCollab email").Content.PlainText,
            ConfirmationTokenRegex(),
            "email confirmation token");

    public static string GetLoginCode(this PostgresIntegrationApiFactory factory, string email) =>
        ExtractMatch(
            factory.EmailSender.GetLatest(email, "Your EduCollab sign-in code").Content.PlainText,
            LoginCodeRegex(),
            "login code");

    public static string GetInvitationToken(this PostgresIntegrationApiFactory factory, string email) =>
        ExtractMatch(
            factory.EmailSender.GetLatest(email, "Invitation to a EduCollab workspace").Content.PlainText,
            InvitationTokenRegex(),
            "workspace invitation token");

    private static string ExtractMatch(string input, Regex regex, string valueName)
    {
        var match = regex.Match(input);
        if (!match.Success)
            throw new InvalidOperationException($"Could not extract {valueName} from email body: {input}");

        return match.Groups[1].Value.Trim();
    }

    [GeneratedRegex(@"using the token below \(valid for .*?\):\s+(\S+)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ConfirmationTokenRegex();

    [GeneratedRegex(@"Use this token.*?:\s+(\S+)", RegexOptions.Singleline)]
    private static partial Regex PasswordResetTokenRegex();

    [GeneratedRegex(@"Code \(expires.*?\):\s+([0-9]{6})", RegexOptions.Singleline)]
    private static partial Regex LoginCodeRegex();

    [GeneratedRegex(@"Accept using this invitation token.*?:\s+(\S+)", RegexOptions.Singleline)]
    private static partial Regex InvitationTokenRegex();
}
