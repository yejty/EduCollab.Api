namespace EduCollab.Application.Models
{
    public class AssetFolderGroupShare
    {
        public int FolderId { get; set; }
        public int GroupId { get; set; }
        public GroupRole Role { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
