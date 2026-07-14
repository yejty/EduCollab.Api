using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EduCollab.Application.Models;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class WorkspacePresetAuthorizationIntegrationTests
{
    [Fact]
    public async Task OwnerWithEditWorkspace_CanUpdateWorkspaceAndThumbnail()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Editable Workspace",
            "Workspace preset authorization");

        var updateResponse = await ownerClient.PutAsJsonAsync("/api/workspace", new UpdateWorkspaceRequest
        {
            Name = "Renamed Workspace",
            Description = "Updated by owner",
        });
        updateResponse.EnsureSuccessStatusCode();

        using var uploadForm = new MultipartFormDataContent();
        var image = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadForm.Add(image, "file", "thumb.png");

        var putThumbnailResponse = await ownerClient.PutAsync("/api/workspace/thumbnail", uploadForm);
        Assert.Equal(HttpStatusCode.NoContent, putThumbnailResponse.StatusCode);

        var deleteThumbnailResponse = await ownerClient.DeleteAsync("/api/workspace/thumbnail");
        Assert.Equal(HttpStatusCode.NoContent, deleteThumbnailResponse.StatusCode);
    }

    [Fact]
    public async Task ManagerWithoutEditWorkspace_CannotUpdateOrArchiveWorkspaceOrThumbnail()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var managerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var managerEmail = $"manager-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Protected Workspace",
            "Workspace preset authorization");

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = managerEmail,
            Presets = WorkspacePresetTestHelpers.PresetsForRole(WorkspaceRole.Manager),
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(managerEmail);
        var acceptResponse = await managerClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FirstName = "Workspace",
                LastName = "Manager",
                Email = managerEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var managerTokens = await managerClient.LoginAsync(managerEmail, password);
        managerClient.SetBearerToken(managerTokens.AccessToken);

        var updateResponse = await managerClient.PutAsJsonAsync("/api/workspace", new UpdateWorkspaceRequest
        {
            Name = "Blocked Rename",
            Description = "Should not apply",
        });
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);

        var deleteWorkspaceResponse = await managerClient.DeleteAsync("/api/workspace");
        Assert.Equal(HttpStatusCode.Forbidden, deleteWorkspaceResponse.StatusCode);

        using var uploadForm = new MultipartFormDataContent();
        var image = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadForm.Add(image, "file", "thumb.png");

        var putThumbnailResponse = await managerClient.PutAsync("/api/workspace/thumbnail", uploadForm);
        Assert.Equal(HttpStatusCode.Forbidden, putThumbnailResponse.StatusCode);

        var deleteThumbnailResponse = await managerClient.DeleteAsync("/api/workspace/thumbnail");
        Assert.Equal(HttpStatusCode.Forbidden, deleteThumbnailResponse.StatusCode);
    }
}
