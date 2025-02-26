using System.Data;
using Microsoft.AspNetCore.Http;

namespace ElasticsearchDemo.Services.Database
{
    public interface IDapperContext : IDisposable
    {
        IDbConnection Connection { get; }
        IElasticsearchService ElasticsearchService { get; }
        IHttpContextAccessor HttpContextAccessor { get; }
        Task<IDbTransaction> BeginTransactionAsync();
        void CommitTransaction();
        void RollbackTransaction();
    }
} 