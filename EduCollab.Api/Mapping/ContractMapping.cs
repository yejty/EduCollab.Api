using System.Linq;
using EduCollab.Application.Models;
using EduCollab.Application.Models.Users;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Requests.Workspaces;
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
                Name = request.Name
            };
        }

        public static Workspace MapToWorkspace(this UpdateWorkspaceRequest request, int id)
        {
            return new Workspace
            {
                Id = id,
                Name = request.Name
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
                Name = workspace.Name
            };
        }

        public static WorkspaceMemberResponse MapToResponse(this WorkspaceMember workspaceMember)
        {
            return new WorkspaceMemberResponse
            {
                UserId = workspaceMember.UserId,
                Role = workspaceMember.Role,
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

        public static WorkspaceMember MapToWorkspaceMember(this UpdateWorkspaceMemberRequest request, int id)
        {
            return new WorkspaceMember
            {
                UserId = request.UserId,
                WorkspaceId = id,
                Role = request.Role
            };
        }
    }
}
