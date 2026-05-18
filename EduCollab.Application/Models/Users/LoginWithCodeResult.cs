namespace EduCollab.Application.Models.Users
{
    public sealed class LoginWithCodeResult
    {
        public User? User { get; init; }
        public bool IsLocked { get; init; }
        public int? RemainingAttempts { get; init; }
    }
}
