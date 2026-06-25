using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Requests.Assets;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Assets;
using EduCollab.Contracts.Responses.Groups;

namespace EduCollab.Api.Tests.Integration;

public sealed class GroupAssetSharingIntegrationTests
{
    [Fact]
    public async Task HierarchicalGroupLibrary_ParentMembershipInheritsChildAssets_ChildDoesNotSeeParent()
    {
        await using var factory = await PostgresIntegrationApiFactory.CreateInitializedAsync();
        using var ownerClient = factory.CreateClient();
        using var parentMemberClient = factory.CreateClient();
        using var childMemberClient = factory.CreateClient();

        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var parentMemberEmail = $"parent-{Guid.NewGuid():N}@example.com";
        var childMemberEmail = $"child-{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var ownerTokens = await ownerClient.RegisterAndConfirmAsync(factory, "Owner", "User", ownerEmail, password);
        ownerClient.SetBearerToken(ownerTokens.AccessToken);

        await ownerClient.CreateApprovedWorkspaceAsync(
            factory,
            ownerEmail,
            "Hierarchical Group Workspace",
            "Group hierarchy integration test");

        foreach (var (client, email, first, last) in new[]
        {
            (parentMemberClient, parentMemberEmail, "Parent", "Member"),
            (childMemberClient, childMemberEmail, "Child", "Member"),
        })
        {
            factory.EmailSender.Clear();
            var inviteResponse = await ownerClient.PostAsJsonAsync("/api/workspace/invitations", new InviteUserRequest
            {
                Email = email,
                Role = "Viewer",
            });
            Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);

            var invitationToken = factory.GetInvitationToken(email);
            var acceptResponse = await client.PostAsJsonAsync($"/api/workspace-invitations/{invitationToken}/accept", new RegisterUserRequest
            {
                FirstName = first,
                LastName = last,
                Email = email,
                Password = password
            });
            acceptResponse.EnsureSuccessStatusCode();

            var tokens = await client.LoginAsync(email, password);
            client.SetBearerToken(tokens.AccessToken);
        }

        var scienceResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Science",
            Description = "Root science group"
        });
        scienceResponse.EnsureSuccessStatusCode();
        var science = await scienceResponse.ReadAsJsonAsync<GroupResponse>();

        var physicsResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Physics",
            Description = "Science subgroup",
            ParentGroupId = science.Id
        });
        physicsResponse.EnsureSuccessStatusCode();
        var physics = await physicsResponse.ReadAsJsonAsync<GroupResponse>();

        var hiddenResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Hidden",
            Description = "Owner-only root group"
        });
        hiddenResponse.EnsureSuccessStatusCode();
        var hidden = await hiddenResponse.ReadAsJsonAsync<GroupResponse>();

        var parentMe = await parentMemberClient.GetAsync("/api/users/me");
        parentMe.EnsureSuccessStatusCode();
        var parentUser = await parentMe.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        var childMe = await childMemberClient.GetAsync("/api/users/me");
        childMe.EnsureSuccessStatusCode();
        var childUser = await childMe.ReadAsJsonAsync<EduCollab.Contracts.Responses.Users.UserResponse>();

        await ownerClient.PostAsJsonAsync($"/api/workspace/groups/{science.Id}/users", new CreateGroupMemberRequest
        {
            UserId = checked((int)parentUser.Id),
        }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        await ownerClient.PostAsJsonAsync($"/api/workspace/groups/{physics.Id}/users", new CreateGroupMemberRequest
        {
            UserId = checked((int)childUser.Id),
        }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        var scienceAssetResponse = await ownerClient.PostAsJsonAsync("/api/workspace/assets", new CreateAssetRequest
        {
            Name = "Science Asset",
            AssetType = "Package",
            GroupId = science.Id,
        });
        scienceAssetResponse.EnsureSuccessStatusCode();
        var scienceAsset = await scienceAssetResponse.ReadAsJsonAsync<AssetResponse>();

        var physicsAssetResponse = await ownerClient.PostAsJsonAsync("/api/workspace/assets", new CreateAssetRequest
        {
            Name = "Physics Asset",
            AssetType = "Package",
            GroupId = physics.Id,
        });
        physicsAssetResponse.EnsureSuccessStatusCode();
        var physicsAsset = await physicsAssetResponse.ReadAsJsonAsync<AssetResponse>();

        var hiddenAssetResponse = await ownerClient.PostAsJsonAsync("/api/workspace/assets", new CreateAssetRequest
        {
            Name = "Hidden Asset",
            AssetType = "Package",
            GroupId = hidden.Id,
        });
        hiddenAssetResponse.EnsureSuccessStatusCode();

        var parentRootsResponse = await parentMemberClient.GetAsync("/api/workspace/groups?accessible=true");
        parentRootsResponse.EnsureSuccessStatusCode();
        var parentRoots = await parentRootsResponse.ReadAsJsonAsync<GroupsResponse>();
        Assert.Contains(parentRoots.Groups, g => g.Id == science.Id);

        var parentSubgroupsResponse = await parentMemberClient.GetAsync($"/api/workspace/groups?accessible=true&parentGroupId={science.Id}");
        parentSubgroupsResponse.EnsureSuccessStatusCode();
        var parentSubgroups = await parentSubgroupsResponse.ReadAsJsonAsync<GroupsResponse>();
        Assert.Contains(parentSubgroups.Groups, g => g.Id == physics.Id);

        var parentPhysicsAssetsResponse = await parentMemberClient.GetAsync($"/api/workspace/groups/{physics.Id}/assets");
        parentPhysicsAssetsResponse.EnsureSuccessStatusCode();
        var parentPhysicsAssets = await parentPhysicsAssetsResponse.ReadAsJsonAsync<AssetsResponse>();
        Assert.Contains(parentPhysicsAssets.Assets, a => a.Id == physicsAsset.Id);

        var parentScienceAssetsResponse = await parentMemberClient.GetAsync($"/api/workspace/groups/{science.Id}/assets");
        parentScienceAssetsResponse.EnsureSuccessStatusCode();
        var parentScienceAssets = await parentScienceAssetsResponse.ReadAsJsonAsync<AssetsResponse>();
        Assert.Contains(parentScienceAssets.Assets, a => a.Id == scienceAsset.Id);

        var childRootsResponse = await childMemberClient.GetAsync("/api/workspace/groups?accessible=true");
        childRootsResponse.EnsureSuccessStatusCode();
        var childRoots = await childRootsResponse.ReadAsJsonAsync<GroupsResponse>();
        Assert.DoesNotContain(childRoots.Groups, g => g.Id == science.Id);
        Assert.Contains(childRoots.Groups, g => g.Id == physics.Id);

        var childPhysicsAssetsResponse = await childMemberClient.GetAsync($"/api/workspace/groups/{physics.Id}/assets");
        childPhysicsAssetsResponse.EnsureSuccessStatusCode();
        var childPhysicsAssets = await childPhysicsAssetsResponse.ReadAsJsonAsync<AssetsResponse>();
        Assert.Single(childPhysicsAssets.Assets);
        Assert.Equal(physicsAsset.Id, childPhysicsAssets.Assets[0].Id);

        var childScienceAssetsResponse = await childMemberClient.GetAsync($"/api/workspace/groups/{science.Id}/assets");
        Assert.Equal(HttpStatusCode.Forbidden, childScienceAssetsResponse.StatusCode);

        var accessibleAssetsResponse = await parentMemberClient.GetAsync("/api/workspace/assets");
        accessibleAssetsResponse.EnsureSuccessStatusCode();
        var accessibleAssets = await accessibleAssetsResponse.ReadAsJsonAsync<AssetsResponse>();
        Assert.Contains(accessibleAssets.Assets, a => a.Id == scienceAsset.Id);
        Assert.Contains(accessibleAssets.Assets, a => a.Id == physicsAsset.Id);
        Assert.DoesNotContain(accessibleAssets.Assets, a => a.Name == "Hidden Asset");

        var childAccessibleAssetsResponse = await childMemberClient.GetAsync("/api/workspace/assets");
        childAccessibleAssetsResponse.EnsureSuccessStatusCode();
        var childAccessibleAssets = await childAccessibleAssetsResponse.ReadAsJsonAsync<AssetsResponse>();
        Assert.Single(childAccessibleAssets.Assets);
        Assert.Equal(physicsAsset.Id, childAccessibleAssets.Assets[0].Id);
    }
}
