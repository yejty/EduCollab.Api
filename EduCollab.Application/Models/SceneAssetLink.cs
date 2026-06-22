namespace EduCollab.Application.Models
{
    public class SceneAssetLink
    {
        public int SceneId { get; set; }
        public int AssetId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
