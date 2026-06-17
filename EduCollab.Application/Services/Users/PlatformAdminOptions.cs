namespace EduCollab.Application.Services.Users
{
    public sealed class PlatformAdminOptions
    {
        public const string SectionName = "PlatformAdmin";

        public string Email { get; set; } = "admin@educollab.local";

        public string Password { get; set; } = "Admin123!";
    }
}
