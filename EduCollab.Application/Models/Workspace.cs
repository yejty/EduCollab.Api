using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduCollab.Application.Models
{
    /// <summary>
    /// A collaboration space. Users may belong to many workspaces; <see cref="User.WorkspaceId"/> is the active one.
    /// </summary>
    public class Workspace
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public int CreatedByUserId { get; set; }

        public bool IsArchived { get; set; }

    }
}
