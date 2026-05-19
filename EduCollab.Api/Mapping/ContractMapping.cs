using System.Linq;
using EduCollab.Application.Models.Groups;
using EduCollab.Application.Models.Users;
using EduCollab.Application.Models.Workspaces;
using EduCollab.Contracts.Requests.Groups;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
using EduCollab.Contracts.Responses.Groups;
using EduCollab.Contracts.Responses.Users;
using EduCollab.Contracts.Responses.Workspaces;

namespace EduCollab.Api.Mapping
{
    public static class ContractMapping
    {
        public static User MapToUser(this RegisterUserRequest request)
        {
            return new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
            };
        }

        public static User MapToUser(this UpdateUserProfileRequest request, int id)
        {
            return new User
            {
                Id = id,
                FirstName = request.FirstName,
                LastName = request.LastName,
            };
        }

        public static Workspace MapToWorkspace(this CreateWorkspaceRequest request)
        {
            return new Workspace
            {
                Name = request.Name,
                Description = request.Description
            };
        }

        public static Workspace MapToWorkspace(this UpdateWorkspaceRequest request, int id)
        {
            return new Workspace
            {
                Id = id,
                Name = request.Name,
                Description = request.Description
            };
        }

        public static Group MapToGroup(this CreateGroupRequest request)
        {
            return new Group
            {
                Name = request.Name,
                Description = request.Description,
            };
        }

        public static Group MapToGroup(this UpdateGroupRequest request, int groupId)
        {
            return new Group
            {
                Id = groupId,
                Name = request.Name,
                Description = request.Description,
            };
        }

        public static UserResponse MapToResponse(this User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                WorkspaceId = user.WorkspaceId,
            };
        }

        public static TokensResponse MapToResponse(this (string AccessToken, string RefreshToken) tokens)
        {
            return new TokensResponse
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken
            };
        }
        public static WorkspaceResponse MapToResponse(this Workspace workspace)
        {
            return new WorkspaceResponse
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Description = workspace.Description,
                CreatedAtUtc = workspace.CreatedAtUtc,
                UpdatedAtUtc = workspace.UpdatedAtUtc,
                CreatedByUserId = workspace.CreatedByUserId,
                IsArchived = workspace.IsArchived,
            };
        }

        public static WorkspacesResponse MapToResponse(this IEnumerable<Workspace> workspaces)
        {
            return new WorkspacesResponse
            {
                Workspaces = workspaces.Select(w => w.MapToResponse()).ToList(),
            };
        }

        public static WorkspaceMemberResponse MapToResponse(this WorkspaceMember workspaceMember)
        {
            return new WorkspaceMemberResponse
            {
                UserId = workspaceMember.UserId,
                Role = workspaceMember.Role.ToString(),
                JoinedAt = workspaceMember.JoinedAtUtc
            };
        }

        public static WorkspaceMembersResponse MapToResponse(this List<WorkspaceMember> workspaceMembers)
        {
            return new WorkspaceMembersResponse
            {
                Members = workspaceMembers.Select(m => m.MapToResponse()).ToList(),
            };
        }

        public static WorkspaceMember MapToWorkspaceMember(this UpdateWorkspaceMemberRequest request, int id, int userId)
        {
            if (!WorkspaceRoleExtensions.TryFromPersisted(request.Role, out var role))
            {
                throw new ArgumentException($"Invalid workspace role '{request.Role}'.");
            }

            return new WorkspaceMember
            {
                UserId = userId,
                WorkspaceId = id,
                Role = role
            };
        }

        public static GroupResponse MapToResponse(this Group group) 
        {
            return new GroupResponse
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CreatedAtUtc = group.CreatedAtUtc,
                CreatedByUserId = group.CreatedByUserId,
                UserCount = group.UserCount
            };
        }

        public static GroupsResponse MapToResponse(this List<Group> groups)
        {
            return new GroupsResponse
            {
                Groups = groups.Select(g => g.MapToResponse()).ToList()
            };
        }
    }
}
