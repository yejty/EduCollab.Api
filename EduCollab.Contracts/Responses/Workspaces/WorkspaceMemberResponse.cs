
namespace EduCollab.Contracts.Responses.Workspaces
{
    public sealed class WorkspaceMemberResponse
    {
        public int UserId { get; set; }

        public List<string> Parameters { get; set; } = new();

        public string Role { get; set; } = string.Empty;

        public DateTimeOffset? JoinedAt { get; set; }
    }
}
