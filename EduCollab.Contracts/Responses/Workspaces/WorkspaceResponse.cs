using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduCollab.Contracts.Responses.Workspaces
{
    public class WorkspaceResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public string CurrentUserRole { get; set; } = null!; //TODO ENUM

        public int UsersCount { get; set; }
    }
}
