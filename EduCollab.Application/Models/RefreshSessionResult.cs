namespace EduCollab.Application.Models
{
    public sealed class RefreshSessionResult
    {
        public required User User { get; init; }

        public required string RefreshToken { get; init; }
    }
}
