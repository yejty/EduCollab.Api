namespace EduCollab.Application.Models
{
    public class AssetGroupShare
    {
        public int AssetId { get; set; }
        public int GroupId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
