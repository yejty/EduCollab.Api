namespace EduCollab.Application.Repositories.Users
{
    public interface IUserRepository
    {
        Task<UserCredentialRecord?> GetCredentialByEmailAsync(string email, CancellationToken cancellationToken);

        Task<UserCredentialRecord?> GetCredentialByIdAsync(int userId, CancellationToken cancellationToken);

        Task<int> InsertRegisteredUserAsync(string firstName, string lastName, string email, string passwordHash, CancellationToken cancellationToken);
    }
}
