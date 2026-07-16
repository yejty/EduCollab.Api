namespace EduCollab.Application.Security
{
    public interface IContentDownloadTokenService
    {
        string CreateAssetContentToken(int userId, int workspaceId, int sceneId, int assetId, int? flowId = null);

        bool TryValidateAssetContentToken(
            string token,
            int userId,
            out int workspaceId,
            out int sceneId,
            out int assetId,
            out int? flowId);

        string CreateSceneContentToken(int userId, int workspaceId, int flowId, int sceneId);

        bool TryValidateSceneContentToken(
            string token,
            int userId,
            out int workspaceId,
            out int flowId,
            out int sceneId);
    }
}
