using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Requests.Assets;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Assets;
using EduCollab.Contracts.Responses.Groups;
using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Api.Tests.Integration;

public sealed class GroupAssetSharingIntegrationTests
{
    [Fact]
    public async Task GroupScopedAssetLibrary_UsesFolderSharesAndDirectAssetShares()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var memberEmail = $"member-{Guid.NewGuid():N}@example.com";
        const string ownerPassword = "Owner123!";
        const string memberPassword = "Member123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, ownerPassword);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Shared Library Workspace",
            "Group asset sharing integration test");

        factory.EmailSender.Clear();

        var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
        {
            Email = memberEmail,
            Role = "Viewer",
        });
        Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);

        var invitationToken = factory.GetInvitationToken(memberEmail);
        var acceptResponse = await memberClient.PostAsJsonAsync($"/api/workspace-invitations/{invitationToken}/accept", new RegisterUserRequest
        {
            FirstName = "Member",
            LastName = "User",
            Email = memberEmail,
            Password = memberPassword
        });
        acceptResponse.EnsureSuccessStatusCode();

        var memberTokens = await memberClient.LoginAsync(memberEmail, memberPassword);
        memberClient.SetBearerToken(memberTokens.AccessToken);

        var createGroupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Design Team",
            Description = "Shared content recipients"
        });
        createGroupResponse.EnsureSuccessStatusCode();
        var group = await createGroupResponse.ReadAsJsonAsync<GroupResponse>();

        var createPrivateGroupResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Owner Only",
            Description = "Content hidden from the viewer member"
        });
        createPrivateGroupResponse.EnsureSuccessStatusCode();
        var privateGroup = await createPrivateGroupResponse.ReadAsJsonAsync<GroupResponse>();

        var meResponse = await memberClient.GetAsync("/api/users/me");
        meResponse.EnsureSuccessStatusCode();
        var member = await meResponse.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        var addMemberResponse = await ownerClient.PostAsJsonAsync($"/api/workspace/groups/{group.Id}/users", new CreateGroupMemberRequest
        {
            UserId = checked((int)member.Id),
        });
        addMemberResponse.EnsureSuccessStatusCode();

        var sharedFolderResponse = await ownerClient.PostAsJsonAsync("/api/workspace/asset-folders", new CreateAssetFolderRequest
        {
            Name = "Shared Root",
            GroupId = group.Id,
        });
        sharedFolderResponse.EnsureSuccessStatusCode();
        var sharedFolder = await sharedFolderResponse.ReadAsJsonAsync<AssetFolderResponse>();

        var nestedFolderResponse = await ownerClient.PostAsJsonAsync("/api/workspace/asset-folders", new CreateAssetFolderRequest
        {
            Name = "Nested",
            ParentFolderId = sharedFolder.Id,
            GroupId = group.Id,
        });
        nestedFolderResponse.EnsureSuccessStatusCode();
        var nestedFolder = await nestedFolderResponse.ReadAsJsonAsync<AssetFolderResponse>();

        var hiddenFolderResponse = await ownerClient.PostAsJsonAsync("/api/workspace/asset-folders", new CreateAssetFolderRequest
        {
            Name = "Hidden Root",
            GroupId = privateGroup.Id,
        });
        hiddenFolderResponse.EnsureSuccessStatusCode();
        var hiddenFolder = await hiddenFolderResponse.ReadAsJsonAsync<AssetFolderResponse>();

        var inheritedAssetResponse = await ownerClient.PostAsJsonAsync("/api/workspace/assets", new CreateAssetRequest
        {
            Name = "Inherited Asset",
            FolderId = nestedFolder.Id,
            AssetType = "Model",
            GroupId = group.Id,
        });
        inheritedAssetResponse.EnsureSuccessStatusCode();
        var inheritedAsset = await inheritedAssetResponse.ReadAsJsonAsync<AssetResponse>();

        var directFolderAssetResponse = await ownerClient.PostAsJsonAsync("/api/workspace/assets", new CreateAssetRequest
        {
            Name = "Direct Hidden Asset",
            FolderId = hiddenFolder.Id,
            AssetType = "Texture",
            GroupId = privateGroup.Id,
        });
        directFolderAssetResponse.EnsureSuccessStatusCode();
        var directFolderAsset = await directFolderAssetResponse.ReadAsJsonAsync<AssetResponse>();

        var directRootAssetResponse = await ownerClient.PostAsJsonAsync("/api/workspace/assets", new CreateAssetRequest
        {
            Name = "Direct Root Asset",
            AssetType = "Model",
            GroupId = group.Id,
        });
        directRootAssetResponse.EnsureSuccessStatusCode();
        var directRootAsset = await directRootAssetResponse.ReadAsJsonAsync<AssetResponse>();

        var unsharedAssetResponse = await ownerClient.PostAsJsonAsync("/api/workspace/assets", new CreateAssetRequest
        {
            Name = "Unshared Asset",
            AssetType = "Model",
            GroupId = privateGroup.Id,
        });
        unsharedAssetResponse.EnsureSuccessStatusCode();

        var shareHiddenAssetResponse = await ownerClient.PostAsJsonAsync($"/api/workspace/assets/{directFolderAsset.Id}/groups", new ShareWithGroupRequest
        {
            GroupId = group.Id,
        });
        shareHiddenAssetResponse.EnsureSuccessStatusCode();
        var sharedHiddenAsset = await shareHiddenAssetResponse.ReadAsJsonAsync<AssetResponse>();
        Assert.Contains(group.Id, sharedHiddenAsset.GroupIds);

        var visibleRootFoldersResponse = await memberClient.GetAsync($"/api/workspace/asset-folders?groupId={group.Id}");
        visibleRootFoldersResponse.EnsureSuccessStatusCode();
        var visibleRootFolders = await visibleRootFoldersResponse.ReadAsJsonAsync<AssetFoldersResponse>();
        Assert.Collection(
            visibleRootFolders.Folders,
            folder =>
            {
                Assert.Equal(sharedFolder.Id, folder.Id);
                Assert.Contains(group.Id, folder.GroupIds);
            });

        var visibleNestedFoldersResponse = await memberClient.GetAsync($"/api/workspace/asset-folders?groupId={group.Id}&parentFolderId={sharedFolder.Id}");
        visibleNestedFoldersResponse.EnsureSuccessStatusCode();
        var visibleNestedFolders = await visibleNestedFoldersResponse.ReadAsJsonAsync<AssetFoldersResponse>();
        Assert.Collection(
            visibleNestedFolders.Folders,
            folder => Assert.Equal(nestedFolder.Id, folder.Id));

        var folderAssetsResponse = await memberClient.GetAsync($"/api/workspace/assets?groupId={group.Id}&folderId={nestedFolder.Id}");
        folderAssetsResponse.EnsureSuccessStatusCode();
        var folderAssets = await folderAssetsResponse.ReadAsJsonAsync<AssetsResponse>();
        Assert.Collection(
            folderAssets.Assets,
            asset => Assert.Equal(inheritedAsset.Id, asset.Id));

        var rootAssetsResponse = await memberClient.GetAsync($"/api/workspace/assets?groupId={group.Id}");
        rootAssetsResponse.EnsureSuccessStatusCode();
        var rootAssets = await rootAssetsResponse.ReadAsJsonAsync<AssetsResponse>();

        Assert.Equal(2, rootAssets.Assets.Count);
        Assert.Contains(rootAssets.Assets, asset => asset.Id == directFolderAsset.Id && asset.FolderId == hiddenFolder.Id);
        Assert.Contains(rootAssets.Assets, asset => asset.Id == directRootAsset.Id && asset.FolderId is null);
        Assert.DoesNotContain(rootAssets.Assets, asset => asset.Id == inheritedAsset.Id);
        Assert.DoesNotContain(rootAssets.Assets, asset => asset.Name == "Unshared Asset");
    }
}
