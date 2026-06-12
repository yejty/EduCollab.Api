namespace EduCollab.Contracts.Responses.Assets
{
    public class AssetFolderResponse
    {
        public int Id { get; set; }
        public int WorkspaceId { get; set; }
        public int? ParentFolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public List<int> GroupIds { get; set; } = new();
        public bool CanManage { get; set; }
    }
}
