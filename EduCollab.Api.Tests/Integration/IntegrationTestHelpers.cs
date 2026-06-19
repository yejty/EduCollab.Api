using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Users;
using EduCollab.Contracts.Responses.Workspaces;

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

    public static string GetInvitationToken(this PostgresIntegrationApiFactory factory, string email)
    {
        var plain = factory.EmailSender.GetLatest(email, "Invitation to a EduCollab workspace").Content.PlainText;
        var fromUrl = InvitationUrlTokenRegex().Match(plain);
        if (fromUrl.Success)
        {
            return Uri.UnescapeDataString(fromUrl.Groups[1].Value.Trim());
        }

        return ExtractMatch(plain, InvitationTokenFallbackRegex(), "workspace invitation token");
    }

    public static string GetWorkspaceCreationApprovalToken(this PostgresIntegrationApiFactory factory, string email)
    {
        var plain = factory.EmailSender.GetLatest(email, "workspace request was approved").Content.PlainText;
        var fromUrl = InvitationUrlTokenRegex().Match(plain);
        if (fromUrl.Success)
        {
            return Uri.UnescapeDataString(fromUrl.Groups[1].Value.Trim());
        }

        return ExtractMatch(plain, WorkspaceCreationApprovalTokenRegex(), "workspace creation approval token");
    }

    public static string GetWorkspaceCreationAdminApprovePath(this PostgresIntegrationApiFactory factory)
    {
        var plain = factory.EmailSender.GetLatest("admin@educollab.local", "New EduCollab workspace creation request").Content.PlainText;
        return ExtractMatch(plain, AdminApprovePathRegex(), "admin approve link");
    }

    public static string GetWorkspaceCreationAdminDenyPath(this PostgresIntegrationApiFactory factory)
    {
        var plain = factory.EmailSender.GetLatest("admin@educollab.local", "New EduCollab workspace creation request").Content.PlainText;
        return ExtractMatch(plain, AdminDenyPathRegex(), "admin deny link");
    }

    public static async Task<WorkspaceResponse> CreateApprovedWorkspaceAsync(
        this HttpClient userClient,
        PostgresIntegrationApiFactory factory,
        string userEmail,
        string name,
        string? description = null)
    {
        var requestResponse = await userClient.PostAsJsonAsync("/api/workspace/creation-requests", new RequestWorkspaceCreationRequest
        {
            Name = name,
            Description = description,
        });
        requestResponse.EnsureSuccessStatusCode();

        using var reviewClient = factory.CreateClient();
        var approvePath = factory.GetWorkspaceCreationAdminApprovePath();
        var approveResponse = await reviewClient.GetAsync(approvePath);
        approveResponse.EnsureSuccessStatusCode();

        var approvalToken = factory.GetWorkspaceCreationApprovalToken(userEmail);

        var createResponse = await userClient.PostAsJsonAsync("/api/workspace", new CreateWorkspaceRequest
        {
            Name = name,
            Description = description,
            ApprovalToken = approvalToken,
        });
        createResponse.EnsureSuccessStatusCode();
        return await createResponse.ReadAsJsonAsync<WorkspaceResponse>();
    }

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

    [GeneratedRegex(@"[?&]token=([^&\s\r\n]+)", RegexOptions.IgnoreCase)]
    private static partial Regex InvitationUrlTokenRegex();

    [GeneratedRegex(@"Use this invitation token.*?:\s+(\S+)", RegexOptions.Singleline)]
    private static partial Regex InvitationTokenFallbackRegex();

    [GeneratedRegex(@"Use this approval token.*?:\s+(\S+)", RegexOptions.Singleline)]
    private static partial Regex WorkspaceCreationApprovalTokenRegex();

    [GeneratedRegex(@"(/api/workspace-creation-review/[^/\s]+/approve)", RegexOptions.IgnoreCase)]
    private static partial Regex AdminApprovePathRegex();

    [GeneratedRegex(@"(/api/workspace-creation-review/[^/\s]+/deny)", RegexOptions.IgnoreCase)]
    private static partial Regex AdminDenyPathRegex();
}
