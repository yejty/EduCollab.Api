using EduCollab.Application.Exceptions;
using EduCollab.Application.Identity;
using EduCollab.Application.Models;
using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Auth;
using EduCollab.Application.Services.Groups;
using EduCollab.Application.Services.Notifications;
using EduCollab.Application.Services.Users;
using EduCollab.Application.Services.Workspaces;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EduCollab.Api.Tests;

public sealed class WorkspaceServiceMultiOwnerTests
{
    private const int WorkspaceId = 1;
    private const int FirstOwnerId = 10;
    private const int SecondOwnerId = 20;
    private const int ManagerId = 30;

    [Fact]
    public async Task RemoveWorkspaceMemberAsync_AllowsOwnerToLeave_WhenAnotherOwnerRemains()
    {
        var repository = CreateRepository(
            OwnerMember(FirstOwnerId),
            OwnerMember(SecondOwnerId));
        var service = CreateService(FirstOwnerId, repository);

        await service.RemoveWorkspaceMemberAsync(WorkspaceId, FirstOwnerId, CancellationToken.None);

        Assert.DoesNotContain(repository.Members, member => member.UserId == FirstOwnerId);
        Assert.Contains(repository.Members, member => member.UserId == SecondOwnerId && member.Role == WorkspaceRole.Owner);
    }

    [Fact]
    public async Task RemoveWorkspaceMemberAsync_BlocksLastOwnerFromLeaving()
    {
        var repository = CreateRepository(OwnerMember(FirstOwnerId));
        var service = CreateService(FirstOwnerId, repository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RemoveWorkspaceMemberAsync(WorkspaceId, FirstOwnerId, CancellationToken.None));

        Assert.Contains("last workspace owner", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(repository.Members, member => member.UserId == FirstOwnerId);
    }

    [Fact]
    public async Task RemoveWorkspaceMemberAsync_AllowsOwnerToRemoveAnotherOwner_WhenAnotherRemains()
    {
        var repository = CreateRepository(
            OwnerMember(FirstOwnerId),
            OwnerMember(SecondOwnerId));
        var service = CreateService(FirstOwnerId, repository);

        await service.RemoveWorkspaceMemberAsync(WorkspaceId, SecondOwnerId, CancellationToken.None);

        Assert.DoesNotContain(repository.Members, member => member.UserId == SecondOwnerId);
    }

    [Fact]
    public async Task RemoveWorkspaceMemberAsync_BlocksManagerFromRemovingOwner()
    {
        var repository = CreateRepository(
            OwnerMember(FirstOwnerId),
            OwnerMember(SecondOwnerId),
            ManagerMember(ManagerId));
        var service = CreateService(ManagerId, repository);

        var exception = await Assert.ThrowsAsync<AccessDeniedException>(() =>
            service.RemoveWorkspaceMemberAsync(WorkspaceId, SecondOwnerId, CancellationToken.None));

        Assert.Contains("Only workspace owners can remove other owners", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateWorkspaceMemberAsync_AllowsPromotingSecondOwner_WithoutDemotingExisting()
    {
        var repository = CreateRepository(
            OwnerMember(FirstOwnerId),
            ManagerMember(ManagerId));
        var service = CreateService(FirstOwnerId, repository);

        var updated = await service.UpdateWorkspaceMemberAsync(
            WorkspaceId,
            ManagerId,
            OwnerMember(ManagerId),
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(WorkspaceRole.Owner, updated.Role);
        Assert.Equal(2, repository.Members.Count(member => member.Role == WorkspaceRole.Owner));
    }

    [Fact]
    public async Task UpdateWorkspaceMemberAsync_BlocksDemotingLastOwner()
    {
        var repository = CreateRepository(OwnerMember(FirstOwnerId));
        var service = CreateService(FirstOwnerId, repository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateWorkspaceMemberAsync(
                WorkspaceId,
                FirstOwnerId,
                ManagerMember(FirstOwnerId),
                CancellationToken.None));

        Assert.Contains("last workspace owner", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(WorkspaceRole.Owner, repository.Members.Single().Role);
    }

    private static WorkspaceService CreateService(int currentUserId, StubWorkspaceRepository repository)
    {
        return new WorkspaceService(
            new StubUserRepository(currentUserId),
            repository,
            new UnusedGroupRepository(),
            new UnusedGroupAccessResolver(),
            new UnusedCreationRequestRepository(),
            new StubCurrentUser(currentUserId),
            Options.Create(new WorkspaceInvitationSettings()),
            new UnusedNotificationService(),
            new StubHostEnvironment(),
            NullLogger<WorkspaceService>.Instance);
    }

    private static StubWorkspaceRepository CreateRepository(params WorkspaceMember[] members) =>
        new(members);

    private static WorkspaceMember OwnerMember(int userId) => new()
    {
        WorkspaceId = WorkspaceId,
        UserId = userId,
        Role = WorkspaceRole.Owner,
        Parameters = WorkspacePermissionParameters.GetParameterKeysForRole(WorkspaceRole.Owner),
        JoinedAtUtc = DateTime.UtcNow,
    };

    private static WorkspaceMember ManagerMember(int userId) => new()
    {
        WorkspaceId = WorkspaceId,
        UserId = userId,
        Role = WorkspaceRole.Manager,
        Parameters = WorkspacePermissionParameters.GetParameterKeysForRole(WorkspaceRole.Manager),
        JoinedAtUtc = DateTime.UtcNow,
    };

    private sealed class StubCurrentUser(int userId) : ICurrentUser
    {
        public int? UserId => userId;
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "EduCollab.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
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

        public Task<int> InsertRegisteredUserAsync(string fullName, string email, string passwordHash, DateTime? EmailConfirmedAtUtc, CancellationToken cancellationToken) =>
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

    private sealed class StubWorkspaceRepository : IWorkspaceRepository
    {
        public List<WorkspaceMember> Members { get; }

        public StubWorkspaceRepository(IEnumerable<WorkspaceMember> members)
        {
            Members = members.Select(Clone).ToList();
        }

        public Task<Workspace?> GetWorkspaceByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult<Workspace?>(id == WorkspaceId
                ? new Workspace { Id = WorkspaceId, Name = "Test" }
                : null);

        public Task<List<WorkspaceMember>> GetWorkspaceMembersAsync(int workspaceId, CancellationToken cancellationToken) =>
            Task.FromResult(Members.Where(member => member.WorkspaceId == workspaceId).Select(Clone).ToList());

        public Task<WorkspaceMember?> GetWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
            Task.FromResult(Members
                .Where(member => member.WorkspaceId == workspaceId && member.UserId == userId)
                .Select(Clone)
                .FirstOrDefault());

        public Task<bool> RemoveWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            var removed = Members.RemoveAll(member => member.WorkspaceId == workspaceId && member.UserId == userId);
            return Task.FromResult(removed > 0);
        }

        public Task<WorkspaceMember?> UpdateWorkspaceMemberAsync(int id, int userId, WorkspaceMember member, CancellationToken cancellationToken)
        {
            var index = Members.FindIndex(existing => existing.WorkspaceId == id && existing.UserId == userId);
            if (index < 0)
            {
                return Task.FromResult<WorkspaceMember?>(null);
            }

            var updated = Clone(member);
            updated.WorkspaceId = id;
            updated.UserId = userId;
            updated.Role = WorkspacePermissionParameters.ResolveMemberRole(updated);
            Members[index] = updated;
            return Task.FromResult<WorkspaceMember?>(Clone(updated));
        }

        public Task<List<Workspace>> GetAllWorkspacesAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Workspace?> UpdateWorkspaceAsync(Workspace workspace, int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> SoftDeleteWorkspaceAsync(int workspaceId, int userId, DateTimeOffset utcNow, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<int> CreateWorkspaceWithOwnerAsync(Workspace workspace, int ownerUserId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<WorkspaceMember>> GetWorkspaceMembershipsForUserAsync(int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<Workspace>> GetWorkspacesForUserAsync(int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> IsUserWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> IsEmailMemberOfWorkspaceAsync(int workspaceId, string email, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task RevokePendingWorkspaceInvitationsAsync(int workspaceId, string email, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<long> InsertWorkspaceInvitationAsync(
            int workspaceId,
            string email,
            string tokenHashSha256Hex,
            WorkspaceRole role,
            IReadOnlySet<string> parameters,
            int groupId,
            DateTimeOffset expiresAtUtc,
            DateTimeOffset createdAtUtc,
            int invitedByUserId,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkspaceInvitationDetails?> GetActiveWorkspaceInvitationAsync(
            string tokenHashSha256Hex,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<int?> AcceptWorkspaceInvitationAndRegisterUserAsync(
            int workspaceId,
            string tokenHashSha256Hex,
            string email,
            string fullName,
            string plainPassword,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkspaceMember?> AcceptWorkspaceInvitationForExistingUserAsync(
            int workspaceId,
            string tokenHashSha256Hex,
            int userId,
            string email,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        private static WorkspaceMember Clone(WorkspaceMember member) => new()
        {
            WorkspaceId = member.WorkspaceId,
            UserId = member.UserId,
            Role = member.Role,
            JoinedAtUtc = member.JoinedAtUtc,
            Parameters = new HashSet<string>(member.Parameters, StringComparer.OrdinalIgnoreCase),
        };
    }

    private sealed class UnusedGroupRepository : IGroupRepository
    {
        public Task<int> CreateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> DeleteGroupAsync(int workspaceId, int groupId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<Group>> GetAllGroupsAsync(int workspaceId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<Group>> GetGroupsForMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<Group>> GetChildGroupsAsync(int workspaceId, int? parentGroupId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Group?> GetGroupByIdAsync(int workspaceId, int groupId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Group?> UpdateGroupAsync(int workspaceId, Group group, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<GroupMember>> GetAllGroupMembersAsync(int workspaceId, int groupId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<GroupMember?> GetGroupMemberAsync(int workspaceId, int groupId, int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<GroupMember?> CreateGroupMemberAsync(int workspaceId, GroupMember member, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> DeleteGroupMemberAsync(int workspaceId, int groupId, int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<int>> GetUserGroupIdsAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class UnusedGroupAccessResolver : IGroupAccessResolver
    {
        public Task<HashSet<int>> GetEffectiveAccessibleGroupIdsAsync(int workspaceId, int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> HasEffectiveAccessAsync(int workspaceId, int userId, int groupId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class UnusedCreationRequestRepository : IWorkspaceCreationRequestRepository
    {
        public Task<long> InsertRequestAsync(WorkspaceCreationRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkspaceCreationRequest?> GetRequestByIdAsync(long requestId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkspaceCreationRequest?> GetLatestRequestForUserAsync(int userId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<List<WorkspaceCreationRequest>> GetRequestsByStatusAsync(WorkspaceCreationRequestStatus? status, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkspaceCreationRequest?> ApproveRequestAsync(
            long requestId,
            int reviewerUserId,
            string tokenHashSha256Hex,
            DateTimeOffset expiresAtUtc,
            DateTimeOffset reviewedAtUtc,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkspaceCreationRequest?> DenyRequestAsync(
            long requestId,
            int reviewerUserId,
            string? denialReason,
            DateTimeOffset reviewedAtUtc,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<WorkspaceCreationRequest?> ConsumeApprovalTokenAsync(
            int userId,
            string tokenHashSha256Hex,
            string workspaceName,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task InsertAdminReviewTokensAsync(
            long requestId,
            string approveTokenHashSha256Hex,
            string denyTokenHashSha256Hex,
            DateTimeOffset expiresAtUtc,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<long?> ConsumeAdminReviewTokenAsync(
            string tokenHashSha256Hex,
            WorkspaceCreationAdminReviewAction action,
            long expectedRequestId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task InvalidateAdminReviewTokensForRequestAsync(long requestId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class UnusedNotificationService : INotificationService
    {
        public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
