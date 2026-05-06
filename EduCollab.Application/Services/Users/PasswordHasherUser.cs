namespace EduCollab.Application.Services.Users
{
    /// <summary>
    /// Marker type for PasswordHasher in Microsoft.AspNetCore.Identity.
    /// </summary>
    public sealed class PasswordHasherUser
    {
        public string Id { get; set; } = string.Empty;
    }
}
