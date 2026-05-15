using System.Data;
using Dapper;
using EduCollab.Contracts.Workspaces;

namespace EduCollab.Infrastructure.Database
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
                return WorkspaceRoleExtensions.FromPersistedOrMember(s);
            }

            return WorkspaceRole.Member;
        }
    }
}
