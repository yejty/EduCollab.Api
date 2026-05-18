namespace EduCollab.Application.Services.Auth
{
    public sealed class RefreshTokenSettings
    {
        public const string SectionName = "Jwt";
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}
