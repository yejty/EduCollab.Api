using System.Data;

namespace EduCollab.Infrastructure.Database
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync();
    }
}
