namespace EduCollab.Application.Services.Auth
{
    public sealed class RefreshTokenSettings
    {
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}
