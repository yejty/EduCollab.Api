using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Groups;
using EduCollab.Application.Services.Users;

namespace EduCollab.Api.Tests;

public sealed class GroupServiceMembershipAuthorizationTests
{
    private const int WorkspaceId = 1;
    private const int GroupId = 10;
    private const int OwnerUserId = 1;
    private const int ManagerUserId = 2;
    private const int OtherUserId = 3;

    [Fact]
    public async Task CreateGroupMemberAsync_WhenManagerNotInGroup_ThrowsAccessDenied()
    {
        var service = CreateService(
            currentUserId: ManagerUserId,
            workspaceRole: WorkspaceRole.Manager,
            groupMembers: []);

        var exception = await Assert.ThrowsAsync<AccessDeniedException>(() =>
            service.CreateGroupMemberAsync(
                GroupId,
                new GroupMember { UserId = ManagerUserId },
                CancellationToken.None));

        Assert.Contains("members of this group", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateGroupMemberAsync_WhenManagerInGroup_AllowsAddingOtherUsers()
    {
        var repository = new StubGroupRepository
        {
            GroupMembers =
            [
                new GroupMember { GroupId = GroupId, UserId = ManagerUserId },
            ],
        };
        var service = CreateService(
            currentUserId: ManagerUserId,
            workspaceRole: WorkspaceRole.Manager,
            repository: repository,
            additionalWorkspaceMemberIds: OtherUserId);

        var created = await service.CreateGroupMemberAsync(
            GroupId,
            new GroupMember { UserId = OtherUserId },
            CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal(OtherUserId, created.UserId);
    }

    [Fact]
    public async Task CreateGroupMemberAsync_WhenOwnerNotInGroup_AllowsAddingMembers()
    {
        var service = CreateService(
            currentUserId: OwnerUserId,
            workspaceRole: WorkspaceRole.Owner,
            groupMembers: [],
            additionalWorkspaceMemberIds: ManagerUserId);

        var created = await service.CreateGroupMemberAsync(
            GroupId,
            new GroupMember { UserId = ManagerUserId },
            CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal(ManagerUserId, created.UserId);
    }

    private static GroupService CreateService(
        int currentUserId,
        WorkspaceRole workspaceRole,
        List<GroupMember>? groupMembers = null,
        StubGroupRepository? repository = null,
        params int[] additionalWorkspaceMemberIds)
    {
        repository ??= new StubGroupRepository { GroupMembers = groupMembers ?? [] };

        var membership = new WorkspaceMember
        {
            WorkspaceId = WorkspaceId,
            UserId = currentUserId,
            Role = workspaceRole,
        };

        var workspaceMemberIds = new HashSet<int>(additionalWorkspaceMemberIds) { currentUserId };

        return new GroupService(
            repository,
            new GroupAccessResolver(repository),
            new StubWorkspaceRepository(membership, workspaceMemberIds),
            new StubUserRepository(currentUserId),
            new StubCurrentUser(currentUserId));
    }

    private sealed class StubCurrentUser(int userId) : ICurrentUser
    {
        public int? UserId => userId;
    }

    private sealed class StubUserRepository(int userId) : IUserRepository
    {
        public Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult<User?>(id == userId
                ? new User { Id = userId, WorkspaceId = WorkspaceId }
                : null);

        public Task RevokeActivePasswordResetTokensForUserAsync(int userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task InsertPasswordResetTokenAsync(int userId, string tokenHashSha256Hex, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<int?> GetUserIdForActivePasswordResetTokenAsync(string email, string tokenHashSha256Hex, DateTimeOffset utcNow, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<int?> CompletePasswordResetAsync(string email, string tokenHashSha256Hex, string newPasswordHash, DateTimeOffset utcNow, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task RevokeActiveLoginCodesForUserAsync(int userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task InsertLoginCodeAsync(int userId, string codeHashSha256Hex, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<LoginCodeConsumeResult> ConsumeLoginCodeAsync(string email, string codeHashSha256Hex, DateTimeOffset utcNow, int maxAttempts, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> ExistsByIdAsync(int id, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<UserCredentialRecordDto?> GetCredentialByEmailAsync(string email, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<UserCredentialRecordDto?> GetCredentialByIdAsync(int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> IsPlatformAdminAsync(int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<int> InsertRegisteredUserAsync(string firstName, string lastName, string email, string passwordHash, DateTime? EmailConfirmedAtUtc, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> UpdateAsync(User user, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> SetActiveWorkspaceIdAsync(int userId, int? workspaceId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> DeleteUserByIdAsync(int id, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task RevokeActiveEmailConfirmationTokensForUserAsync(int userId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task InsertEmailConfirmationTokenAsync(int userId, string tokenHashSha256Hex, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<int?> GetUserIdForActiveEmailConfirmationTokenAsync(string email, string tokenHashSha256Hex, DateTimeOffset utcNow, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<int?> ConfirmEmailAsync(string email, string tokenHashSha256Hex, DateTimeOffset utcNow, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class StubWorkspaceRepository(WorkspaceMember membership, HashSet<int> workspaceMemberIds) : IWorkspaceRepository
    {
        public Task<WorkspaceMember?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
            Task.FromResult<WorkspaceMember?>(
                workspaceId == membership.WorkspaceId && userId == membership.UserId ? membership : null);

        public Task<List<WorkspaceMember>> GetWorkspaceMembershipsForUserAsync(int userId, CancellationToken cancellationToken) =>
            Task.FromResult(userId == membership.UserId ? new List<WorkspaceMember> { membership } : []);

        public Task<bool> IsUserWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
            Task.FromResult(workspaceId == membership.WorkspaceId && workspaceMemberIds.Contains(userId));

        public Task<Workspace?> GetWorkspaceByIdAsync(int id, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<Workspace>> GetAllWorkspacesAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Workspace?> UpdateWorkspaceAsync(Workspace workspace, int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> SoftDeleteWorkspaceAsync(int workspaceId, int userId, DateTimeOffset utcNow, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<WorkspaceMember>> GetWorkspaceMembersAsync(int workspaceId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<int> CreateWorkspaceWithOwnerAsync(Workspace workspace, int ownerUserId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<Workspace>> GetWorkspacesForUserAsync(int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> IsEmailMemberOfWorkspaceAsync(int workspaceId, string email, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task RevokePendingWorkspaceInvitationsAsync(int workspaceId, string email, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task InsertWorkspaceInvitationAsync(int workspaceId, string email, string tokenHashSha256Hex, WorkspaceRole role, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc, int invitedByUserId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkspaceInvitationDetails?> GetActiveWorkspaceInvitationAsync(string tokenHashSha256Hex, DateTimeOffset utcNow, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<int?> AcceptWorkspaceInvitationAndRegisterUserAsync(int workspaceId, string tokenHashSha256Hex, string email, string firstName, string lastName, string plainPassword, DateTimeOffset utcNow, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkspaceMember?> AcceptWorkspaceInvitationForExistingUserAsync(int workspaceId, string tokenHashSha256Hex, int userId, string email, DateTimeOffset utcNow, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> RemoveWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkspaceMember?> UpdateWorkspaceMemberAsync(int id, int userId, WorkspaceMember member, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task DemoteWorkspaceOwnersExceptAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class StubGroupRepository : IGroupRepository
    {
        public List<GroupMember> GroupMembers { get; init; } = [];

        public Task<Group?> GetGroupByIdAsync(int workspaceId, int groupId, CancellationToken cancellationToken) =>
            Task.FromResult<Group?>(
                workspaceId == WorkspaceId && groupId == GroupId
                    ? new Group { Id = GroupId, Name = "Test Group" }
                    : null);

        public Task<GroupMember?> GetGroupMemberAsync(int workspaceId, int groupId, int userId, CancellationToken cancellationToken) =>
            Task.FromResult(GroupMembers.FirstOrDefault(m =>
                m.GroupId == groupId && m.UserId == userId));

        public Task<GroupMember?> CreateGroupMemberAsync(int workspaceId, GroupMember member, CancellationToken cancellationToken)
        {
            member.GroupId = GroupId;
            GroupMembers.Add(member);
            return Task.FromResult<GroupMember?>(member);
        }

        public Task<List<int>> GetUserGroupIdsAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
            Task.FromResult(GroupMembers.Where(m => m.UserId == userId).Select(m => m.GroupId).ToList());

        public Task<int> CreateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> DeleteGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<Group>> GetAllGroupsAsync(int workspaceId, CancellationToken cancellationToken) =>
            Task.FromResult(new List<Group>());

        public Task<List<Group>> GetGroupsForMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<Group>> GetChildGroupsAsync(int workspaceId, int? parentGroupId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Group?> UpdateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<GroupMember>> GetAllGroupMembersAsync(int workspaceId, int groupId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> DeleteGroupMemberAsync(int workspaceId, int groupId, int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
