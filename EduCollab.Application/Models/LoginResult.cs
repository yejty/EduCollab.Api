namespace EduCollab.Application.Models
{
    public sealed class LoginResult
    {
        public User? User { get; init; }
        public bool UserNotFound { get; init; }
    }
}
