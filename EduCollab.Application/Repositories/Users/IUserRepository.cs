using EduCollab.Application.Models.Users;

namespace EduCollab.Application.Repositories.Users
{
    public interface IUserRepository
    {
        Task<bool> ExistsByIdAsync(int id, CancellationToken cancellationToken);
        Task<UserCredentialRecordDto?> GetCredentialByEmailAsync(string email, CancellationToken cancellationToken);

        Task<UserCredentialRecordDto?> GetCredentialByIdAsync(int userId, CancellationToken cancellationToken);
        Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken);
        Task<int> InsertRegisteredUserAsync(string firstName, string lastName, string email, string passwordHash, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(User user, CancellationToken cancellationToken);
    }
}
