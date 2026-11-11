using System.Data;
using Dapper;
using Space.Application.Models;

namespace Space.Infrastructure.Database
{
    internal sealed class WorkspaceRoleTypeHandler : SqlMapper.TypeHandler<WorkspaceRole>
    {
        public override void SetValue(IDbDataParameter parameter, WorkspaceRole value)
        {
            parameter.DbType = DbType.String;
            parameter.Value = value.ToPersistedString();
        }

        public override WorkspaceRole Parse(object value)
        {
            if (value is string s)
            {
                return WorkspaceRoleExtensions.FromPersistedOrViewer(s);
            }

            return WorkspaceRole.Viewer;
        }
    }
}
