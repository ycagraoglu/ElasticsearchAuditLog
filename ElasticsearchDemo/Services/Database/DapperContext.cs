using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ElasticsearchDemo.Services.Database
{
    public class DapperContext : IDapperContext
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<DapperContext> _logger;
        private IDbConnection? _connection;
        private IDbTransaction? _transaction;
        private bool _disposed;

        public IElasticsearchService ElasticsearchService { get; }

        public DapperContext(
            IDbConnectionFactory connectionFactory,
            ILogger<DapperContext> logger,
            IElasticsearchService elasticsearchService)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ElasticsearchService = elasticsearchService ?? throw new ArgumentNullException(nameof(elasticsearchService));
        }

        public IDbConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = _connectionFactory.CreateConnection();
                }
                return _connection;
            }
        }

        public async Task<IDbTransaction> BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                return _transaction;
            }

            if (_connection == null)
            {
                _connection = await _connectionFactory.CreateConnectionAsync();
            }

            _transaction = _connection.BeginTransaction();
            return _transaction;
        }

        public void CommitTransaction()
        {
            try
            {
                _transaction?.Commit();
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public void RollbackTransaction()
        {
            try
            {
                _transaction?.Rollback();
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_transaction != null)
                    {
                        try { _transaction.Dispose(); }
                        catch (Exception ex) { _logger.LogError(ex, "Transaction dispose edilirken hata oluştu"); }
                        _transaction = null;
                    }
                    if (_connection != null)
                    {
                        try { _connection.Dispose(); }
                        catch (Exception ex) { _logger.LogError(ex, "Connection dispose edilirken hata oluştu"); }
                        _connection = null;
                    }
                }
                _disposed = true;
            }
        }
    }
} 