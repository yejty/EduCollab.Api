using EduCollab.Application.Models.Users;

namespace EduCollab.Application.Services.Auth
{
    public interface IRefreshTokenService
    {
        Task<string> CreateAsync(int userId, CancellationToken cancellationToken);

        Task<RefreshSessionResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    }
}
