namespace EduCollab.Application.Services.Content
{
    public sealed class WorkspaceContentStorageOptions
    {
        public const string SectionName = "WorkspaceContentStorage";

        public string RootPath { get; set; } = "App_Data/Content";

        public long MaxAssetBytes { get; set; } = 104_857_600;

        public long MaxSceneJsonBytes { get; set; } = 10_485_760;
    }
}
