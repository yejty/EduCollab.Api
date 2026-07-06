
namespace EduCollab.Contracts.Responses.Workspaces
{
    public sealed class WorkspaceMemberResponse
    {
        public int UserId { get; set; }

        public string Preset { get; set; } = string.Empty;

        public bool IsRole { get; set; }

        public DateTimeOffset? JoinedAt { get; set; }
    }
}
