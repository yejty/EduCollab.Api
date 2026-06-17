namespace EduCollab.Application.Models
{
    public class SceneGroupShare
    {
        public int SceneId { get; set; }
        public int GroupId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
