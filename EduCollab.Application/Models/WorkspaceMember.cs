using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduCollab.Application.Models
{
    public class WorkspaceMember
    {
        public int UserId { get; set; }
        public int WorkspaceId { get; set; }
        public string Role { get; set; } = null!;
        public DateTime JoinedAtUtc { get; set; }
    }
}
