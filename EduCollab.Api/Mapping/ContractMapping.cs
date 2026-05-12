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
        public static User MapToUser(this CreateUserRequest request)
        {
            return new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
            };
        }

        public static User MapToUser(this UpdateUserRequest request, int id)
        {
            return new User
            {
                Id = id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
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
                Email = user.Email
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
    }
}
