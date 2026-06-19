using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Api.Tests.Integration;

public sealed class WorkspaceCreationApprovalIntegrationTests
{
    [Fact]
    public async Task WorkspaceCreation_RequiresApprovalToken_FromAdminEmailButtons()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var userClient = factory.CreateClient();
        using var reviewClient = factory.CreateClient();

        var userEmail = $"requester-{Guid.NewGuid():N}@example.com";
        const string userPassword = "Requester123!";

        var userTokens = await userClient.RegisterAndConfirmAsync(factory, "Request", "User", userEmail, userPassword);
        userClient.SetBearerToken(userTokens.AccessToken);

        var createWithoutTokenResponse = await userClient.PostAsJsonAsync("/api/workspace", new CreateWorkspaceRequest
        {
            Name = "Blocked Workspace",
            Description = "Should fail without token",
            ApprovalToken = string.Empty,
        });
        Assert.Equal(HttpStatusCode.BadRequest, createWithoutTokenResponse.StatusCode);

        var submitResponse = await userClient.PostAsJsonAsync("/api/workspace/creation-requests", new RequestWorkspaceCreationRequest
        {
            Name = "Approved Workspace",
            Description = "Pending review",
        });
        submitResponse.EnsureSuccessStatusCode();
        var pendingRequest = await submitResponse.ReadAsJsonAsync<WorkspaceCreationRequestResponse>();
        Assert.Equal("Pending", pendingRequest.Status);

        var adminNotification = factory.EmailSender.GetLatest("admin@educollab.local", "New EduCollab workspace creation request");
        Assert.Contains("Approved Workspace", adminNotification.Content.PlainText, StringComparison.Ordinal);
        Assert.Contains("/approve", adminNotification.Content.PlainText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/deny", adminNotification.Content.PlainText, StringComparison.OrdinalIgnoreCase);

        var approvePath = factory.GetWorkspaceCreationAdminApprovePath();
        var approveResponse = await reviewClient.GetAsync(approvePath);
        approveResponse.EnsureSuccessStatusCode();
        Assert.Contains("Request approved", await approveResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var approvalToken = factory.GetWorkspaceCreationApprovalToken(userEmail);

        var createResponse = await userClient.PostAsJsonAsync("/api/workspace", new CreateWorkspaceRequest
        {
            Name = "Approved Workspace",
            Description = "Pending review",
            ApprovalToken = approvalToken,
        });
        createResponse.EnsureSuccessStatusCode();
        var workspace = await createResponse.ReadAsJsonAsync<WorkspaceResponse>();
        Assert.Equal("Approved Workspace", workspace.Name);
        Assert.Equal("Owner", workspace.CurrentUserRole);
    }

    [Fact]
    public async Task WorkspaceCreationRequest_CanBeDenied_FromAdminEmailButton()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var userClient = factory.CreateClient();
        using var reviewClient = factory.CreateClient();

        var userEmail = $"denied-{Guid.NewGuid():N}@example.com";
        const string userPassword = "Requester123!";

        var userTokens = await userClient.RegisterAndConfirmAsync(factory, "Denied", "User", userEmail, userPassword);
        userClient.SetBearerToken(userTokens.AccessToken);

        var submitResponse = await userClient.PostAsJsonAsync("/api/workspace/creation-requests", new RequestWorkspaceCreationRequest
        {
            Name = "Denied Workspace",
        });
        submitResponse.EnsureSuccessStatusCode();

        var denyPath = factory.GetWorkspaceCreationAdminDenyPath();
        var denyResponse = await reviewClient.GetAsync(denyPath);
        denyResponse.EnsureSuccessStatusCode();
        Assert.Contains("Request denied", await denyResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var denialEmail = factory.EmailSender.GetLatest(userEmail, "Your EduCollab workspace request was denied");
        Assert.Contains("Denied Workspace", denialEmail.Content.PlainText, StringComparison.Ordinal);
    }
}
