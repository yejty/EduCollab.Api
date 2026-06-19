namespace EduCollab.Application.Services.Assets
{
    public static class AssetContentUrls
    {
        public static string GetRelativeUrl(int assetId) => $"/api/workspace/assets/{assetId}/content";
    }
}
