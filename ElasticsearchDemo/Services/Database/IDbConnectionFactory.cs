using System.Data;

namespace ElasticsearchDemo.Services.Database
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync();
        IDbConnection CreateConnection();
    }
} 