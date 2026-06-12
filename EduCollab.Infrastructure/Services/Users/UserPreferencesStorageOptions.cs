namespace EduCollab.Infrastructure.Services.Users
{
    public sealed class UserPreferencesStorageOptions
    {
        public const string SectionName = "UserPreferencesStorage";

        public string RootPath { get; set; } = "App_Data/UserPreferences";
    }
}
