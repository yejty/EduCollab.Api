namespace EduCollab.Application.Repositories
{
    public sealed class StoredRefreshTokenRecord
    {
        public long Id { get; init; }

        public int UserId { get; init; }
    }
}
