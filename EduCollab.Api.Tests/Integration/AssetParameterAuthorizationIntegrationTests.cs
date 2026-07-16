using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Requests.Assets;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Assets;
using EduCollab.Contracts.Responses.Groups;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class AssetParameterAuthorizationIntegrationTests
{
    [Fact]
    public async Task LoadAssetsOnly_CanReadAssets_ButCannotMutate()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"loader-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Load Assets Gate", "Asset parameter authorization");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["loadAssets"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Load Only",
                Email = memberEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, password);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest { Name = "Shared" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var memberMeResponse = await memberClient.GetAsync("/api/users/me");
        memberMeResponse.EnsureSuccessStatusCode();
        var member = await memberMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)member.Id) }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        var asset = await ownerClient.PostAssetAsync("Shared Asset", group.Id);

        var listResponse = await memberClient.GetAsync("/api/workspace/assets");
        listResponse.EnsureSuccessStatusCode();

        var getResponse = await memberClient.GetAsync($"/api/workspace/assets/{asset.Id}");
        getResponse.EnsureSuccessStatusCode();

        var contentResponse = await memberClient.GetAsync($"/api/workspace/assets/{asset.Id}/content");
        contentResponse.EnsureSuccessStatusCode();

        var groupAssetsResponse = await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/assets");
        groupAssetsResponse.EnsureSuccessStatusCode();

        using var createForm = AssetTestHelpers.CreateAssetMultipartForm("Blocked Create");
        var createResponse = await memberClient.PostAsync("/api/workspace/assets", createForm);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);

        var updateResponse = await memberClient.PutAsJsonAsync(
            $"/api/workspace/assets/{asset.Id}",
            new UpdateAssetRequest { Name = "Blocked Update", AssetType = asset.AssetType });
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);

        var deleteResponse = await memberClient.DeleteAsync($"/api/workspace/assets/{asset.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);

        using var putContentForm = new MultipartFormDataContent();
        putContentForm.Add(AssetTestHelpers.CreateMinimalZipFileContent(), "file", "asset.zip");
        var putContentResponse = await memberClient.PutAsync(
            $"/api/workspace/assets/{asset.Id}/content",
            putContentForm);
        Assert.Equal(HttpStatusCode.Forbidden, putContentResponse.StatusCode);

        var addGroupResponse = await memberClient.PostAsJsonAsync(
            "/api/workspace/asset-groups",
            new AttachAssetGroupRequest { AssetId = asset.Id, GroupId = group.Id });
        Assert.Equal(HttpStatusCode.Forbidden, addGroupResponse.StatusCode);

        var setGroupsResponse = await memberClient.PutAsJsonAsync(
            $"/api/workspace/asset-groups?assetId={asset.Id}",
            new SetResourceGroupsRequest { GroupIds = [group.Id] });
        Assert.Equal(HttpStatusCode.Forbidden, setGroupsResponse.StatusCode);

        var removeGroupResponse = await memberClient.DeleteAsync(
            $"/api/workspace/asset-groups?assetId={asset.Id}&groupId={group.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, removeGroupResponse.StatusCode);

        var getAssetGroupsResponse = await memberClient.GetAsync($"/api/workspace/asset-groups?assetId={asset.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getAssetGroupsResponse.StatusCode);
    }

    [Fact]
    public async Task AddAssetsOnly_CanReadAccessibleAssets_AndMutateOwnAssets()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"creator-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Add Assets Gate", "Asset parameter authorization");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["addAssets"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "Add Only",
                Email = memberEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, password);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest { Name = "Shared" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var memberMeResponse = await memberClient.GetAsync("/api/users/me");
        memberMeResponse.EnsureSuccessStatusCode();
        var member = await memberMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)member.Id) }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        var ownerAsset = await ownerClient.PostAssetAsync("Owner Asset", group.Id);

        var listResponse = await memberClient.GetAsync("/api/workspace/assets");
        listResponse.EnsureSuccessStatusCode();

        var getResponse = await memberClient.GetAsync($"/api/workspace/assets/{ownerAsset.Id}");
        getResponse.EnsureSuccessStatusCode();

        var contentResponse = await memberClient.GetAsync($"/api/workspace/assets/{ownerAsset.Id}/content");
        contentResponse.EnsureSuccessStatusCode();

        var groupAssetsResponse = await memberClient.GetAsync($"/api/workspace/groups/{group.Id}/assets");
        groupAssetsResponse.EnsureSuccessStatusCode();

        using var createForm = AssetTestHelpers.CreateAssetMultipartForm("Personal Asset");
        var createResponse = await memberClient.PostAsync("/api/workspace/assets", createForm);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.ReadAsJsonAsync<AssetResponse>();

        var updateResponse = await memberClient.PutAsJsonAsync(
            $"/api/workspace/assets/{created.Id}",
            new UpdateAssetRequest { Name = "Updated Asset", AssetType = created.AssetType });
        updateResponse.EnsureSuccessStatusCode();

        using var replaceContentForm = new MultipartFormDataContent();
        replaceContentForm.Add(AssetTestHelpers.CreateMinimalZipFileContent(), "file", "asset.zip");
        var putContentResponse = await memberClient.PutAsync(
            $"/api/workspace/assets/{created.Id}/content",
            replaceContentForm);
        putContentResponse.EnsureSuccessStatusCode();

        var addGroupResponse = await memberClient.PostAsJsonAsync(
            "/api/workspace/asset-groups",
            new AttachAssetGroupRequest { AssetId = created.Id, GroupId = group.Id });
        Assert.Equal(HttpStatusCode.Created, addGroupResponse.StatusCode);

        var setGroupsResponse = await memberClient.PutAsJsonAsync(
            $"/api/workspace/asset-groups?assetId={created.Id}",
            new SetResourceGroupsRequest { GroupIds = [group.Id] });
        setGroupsResponse.EnsureSuccessStatusCode();

        var getAssetGroupsResponse = await memberClient.GetAsync($"/api/workspace/asset-groups?assetId={created.Id}");
        getAssetGroupsResponse.EnsureSuccessStatusCode();

        var removeGroupResponse = await memberClient.DeleteAsync(
            $"/api/workspace/asset-groups?assetId={created.Id}&groupId={group.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeGroupResponse.StatusCode);

        var deleteResponse = await memberClient.DeleteAsync($"/api/workspace/assets/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ViewerWithoutAssetParameters_CannotReadAssetsByIdOrContent()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var viewerClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var viewerEmail = $"viewer-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(factory, ownerEmail, "Viewer Asset Detail Gate", "Asset parameter authorization");
        var invitationGroup = await ownerClient.CreateGroupAsync();

        factory.EmailSender.Clear();
        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = viewerEmail,
            GroupId = invitationGroup.Id,
            Parameters = ["loadScenes", "loadFlows"],
        });
        inviteResponse.EnsureSuccessStatusCode();

        var invitationToken = factory.GetInvitationToken(viewerEmail);
        var acceptResponse = await viewerClient.PostAsJsonAsync(
            $"/api/workspace-invitations/{invitationToken}/accept",
            new RegisterUserRequest
            {
                FullName = "View Only",
                Email = viewerEmail,
                Password = password,
            });
        acceptResponse.EnsureSuccessStatusCode();

        var viewerTokens = await viewerClient.LoginAsync(viewerEmail, password);
        viewerClient.SetBearerToken(viewerTokens.AccessToken);

        var groupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest { Name = "Team" });
        groupResponse.EnsureSuccessStatusCode();
        var group = await groupResponse.ReadAsJsonAsync<GroupResponse>();

        var viewerMeResponse = await viewerClient.GetAsync("/api/users/me");
        viewerMeResponse.EnsureSuccessStatusCode();
        var viewer = await viewerMeResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        await ownerClient.PostAsJsonAsync(
            $"/api/workspace/groups/{group.Id}/users",
            new CreateGroupMemberRequest { UserId = checked((int)viewer.Id) }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        var asset = await ownerClient.PostAssetAsync("Team Asset", group.Id);

        var getResponse = await viewerClient.GetAsync($"/api/workspace/assets/{asset.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);

        var contentResponse = await viewerClient.GetAsync($"/api/workspace/assets/{asset.Id}/content");
        Assert.Equal(HttpStatusCode.Forbidden, contentResponse.StatusCode);
    }
}
