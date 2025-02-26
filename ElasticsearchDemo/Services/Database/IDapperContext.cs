using System.Data;

namespace ElasticsearchDemo.Services.Database
{
    public interface IDapperContext : IDisposable
    {
        IDbConnection Connection { get; }
        IElasticsearchService ElasticsearchService { get; }
        Task<IDbTransaction> BeginTransactionAsync();
        void CommitTransaction();
        void RollbackTransaction();
    }
} 