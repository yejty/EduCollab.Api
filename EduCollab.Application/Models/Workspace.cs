using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduCollab.Application.Models
{
    /// <summary>
    /// A collaboration space. Users reference at most one workspace via <see cref="Users.User.WorkspaceId"/>.
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
