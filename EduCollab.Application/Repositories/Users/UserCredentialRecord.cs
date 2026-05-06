namespace EduCollab.Application.Repositories.Users
{
    public sealed class UserCredentialRecord
    {
        public int Id { get; init; }

        public string Email { get; init; } = string.Empty;

        public string? PasswordHash { get; init; }
    }
}
