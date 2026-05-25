namespace EduCollab.Application.Repositories
{
    public sealed class LoginCodeConsumeResult
    {
        public int? UserId { get; init; }
        public bool IsLocked { get; init; }
        public int? RemainingAttempts { get; init; }
    }
}
