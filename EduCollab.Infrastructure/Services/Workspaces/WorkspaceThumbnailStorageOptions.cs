namespace EduCollab.Infrastructure.Services.Workspaces
{
    public sealed class WorkspaceThumbnailStorageOptions
    {
        public const string SectionName = "WorkspaceThumbnailStorage";

        public string RootPath { get; set; } = "App_Data/WorkspaceThumbnails";
    }
}
