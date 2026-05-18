namespace EduCollab.Application.Repositories.Users
{
    public sealed class UserCredentialRecordDto
    {
        public int Id { get; init; }

        public string Email { get; init; } = string.Empty;

        public string? PasswordHash { get; init; }

        public DateTime? EmailConfirmedAtUtc { get; init; }
    }
}
