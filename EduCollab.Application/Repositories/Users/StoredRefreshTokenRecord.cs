namespace EduCollab.Application.Repositories.Users
{
    public sealed class StoredRefreshTokenRecord
    {
        public long Id { get; init; }

        public int UserId { get; init; }
    }
}
