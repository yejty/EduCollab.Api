using EduCollab.Application.Models.Users;
using EduCollab.Contracts.Requests.Users;
using EduCollab.Contracts.Responses.Users;

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
    }
}
