using System.Net;
using System.Net.Http.Json;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Assets;
using EduCollab.Contracts.Responses.Groups;

namespace EduCollab.Api.Tests.Integration;

[Trait("Category", "Integration")]
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

        var parentPhysicsMembershipResponse = await ownerClient.GetAsync(
            $"/api/workspace/groups/{physics.Id}/users/{parentUser.Id}");
        parentPhysicsMembershipResponse.EnsureSuccessStatusCode();

        await ownerClient.PostAsJsonAsync($"/api/workspace/groups/{physics.Id}/users", new CreateGroupMemberRequest
        {
            UserId = checked((int)childUser.Id),
        }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        var scienceAsset = await ownerClient.PostAssetAsync("Science Asset", science.Id);

        var physicsAsset = await ownerClient.PostAssetAsync("Physics Asset", physics.Id);

        await ownerClient.PostAssetAsync("Hidden Asset", hidden.Id);

        var parentRootsResponse = await parentMemberClient.GetAsync("/api/workspace/groups");
        parentRootsResponse.EnsureSuccessStatusCode();
        var parentRoots = await parentRootsResponse.ReadAsJsonAsync<GroupsResponse>();
        Assert.Contains(parentRoots.Groups, g => g.Id == science.Id);

        var parentSubgroupsResponse = await parentMemberClient.GetAsync($"/api/workspace/groups?parentId={science.Id}");
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

        var childRootsResponse = await childMemberClient.GetAsync("/api/workspace/groups");
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

        var parentFlatResponse = await parentMemberClient.GetAsync("/api/workspace/groups/flat");
        parentFlatResponse.EnsureSuccessStatusCode();
        var parentFlatGroups = await parentFlatResponse.ReadAsJsonAsync<GroupsResponse>();
        Assert.Contains(parentFlatGroups.Groups, g => g.Id == science.Id);
        Assert.Contains(parentFlatGroups.Groups, g => g.Id == physics.Id);
        Assert.DoesNotContain(parentFlatGroups.Groups, g => g.Id == hidden.Id);

        var childFlatResponse = await childMemberClient.GetAsync("/api/workspace/groups/flat");
        childFlatResponse.EnsureSuccessStatusCode();
        var childFlatGroups = await childFlatResponse.ReadAsJsonAsync<GroupsResponse>();
        Assert.DoesNotContain(childFlatGroups.Groups, g => g.Id == science.Id);
        Assert.Contains(childFlatGroups.Groups, g => g.Id == physics.Id);

        var ownerFlatResponse = await ownerClient.GetAsync("/api/workspace/groups/flat");
        ownerFlatResponse.EnsureSuccessStatusCode();
        var ownerFlatGroups = await ownerFlatResponse.ReadAsJsonAsync<GroupsResponse>();
        Assert.Contains(ownerFlatGroups.Groups, g => g.Id == science.Id);
        Assert.Contains(ownerFlatGroups.Groups, g => g.Id == physics.Id);
        Assert.Contains(ownerFlatGroups.Groups, g => g.Id == hidden.Id);

    }

    [Fact]
    public async Task UpdateGroup_RejectsParentGroupIdThatIsSubgroup()
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
            "Subgroup Parent Validation Workspace",
            "Group parent validation integration test");

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

        var chemistryResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Chemistry",
            Description = "Nested science subgroup",
            ParentGroupId = physics.Id
        });
        chemistryResponse.EnsureSuccessStatusCode();
        var chemistry = await chemistryResponse.ReadAsJsonAsync<GroupResponse>();

        var moveUnderDirectChildResponse = await ownerClient.PutAsJsonAsync(
            $"/api/workspace/groups/{science.Id}",
            new UpdateGroupRequest { ParentGroupId = physics.Id });
        Assert.Equal(HttpStatusCode.BadRequest, moveUnderDirectChildResponse.StatusCode);

        var moveUnderNestedChildResponse = await ownerClient.PutAsJsonAsync(
            $"/api/workspace/groups/{science.Id}",
            new UpdateGroupRequest { ParentGroupId = chemistry.Id });
        Assert.Equal(HttpStatusCode.BadRequest, moveUnderNestedChildResponse.StatusCode);

        var moveUnderSelfResponse = await ownerClient.PutAsJsonAsync(
            $"/api/workspace/groups/{science.Id}",
            new UpdateGroupRequest { ParentGroupId = science.Id });
        Assert.Equal(HttpStatusCode.BadRequest, moveUnderSelfResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteGroup_DeletesAllSubgroups()
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
            "Cascade Delete Workspace",
            "Group cascade delete integration test");

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

        var chemistryResponse = await ownerClient.PostAsJsonAsync("/api/workspace/groups", new CreateGroupRequest
        {
            Name = "Chemistry",
            Description = "Nested science subgroup",
            ParentGroupId = physics.Id
        });
        chemistryResponse.EnsureSuccessStatusCode();
        var chemistry = await chemistryResponse.ReadAsJsonAsync<GroupResponse>();

        var deleteResponse = await ownerClient.DeleteAsync($"/api/workspace/groups/{science.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var allGroupsResponse = await ownerClient.GetAsync("/api/workspace/groups");
        allGroupsResponse.EnsureSuccessStatusCode();
        var allGroups = await allGroupsResponse.ReadAsJsonAsync<GroupsResponse>();
        Assert.DoesNotContain(allGroups.Groups, g => g.Id == science.Id);
        Assert.DoesNotContain(allGroups.Groups, g => g.Id == physics.Id);
        Assert.DoesNotContain(allGroups.Groups, g => g.Id == chemistry.Id);
    }
}
