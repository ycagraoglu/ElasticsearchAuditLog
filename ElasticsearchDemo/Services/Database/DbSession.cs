using System.Data;
using Microsoft.Extensions.Logging;

namespace ElasticsearchDemo.Services.Database
{
    public interface IDapperContext : IDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction? Transaction { get; }
        Task<IDbTransaction> BeginTransactionAsync();
        void CommitTransaction();
        void RollbackTransaction();
    }

    public class DapperContext : IDapperContext
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<DapperContext> _logger;
        private IDbConnection? _connection;
        private IDbTransaction? _transaction;
        private bool _disposed;

        public IDbConnection Connection
        {
            get
            {
                _connection ??= _connectionFactory.CreateConnection();
                return _connection;
            }
        }

        public IDbTransaction? Transaction => _transaction;

        public DapperContext(
            IDbConnectionFactory connectionFactory,
            ILogger<DapperContext> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IDbTransaction> BeginTransactionAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DapperContext));
            }

            if (_transaction != null)
            {
                _logger.LogWarning("Transaction zaten başlatılmış durumda");
                return _transaction;
            }

            if (_connection == null)
            {
                _connection = await _connectionFactory.CreateConnectionAsync();
            }

            _transaction = _connection.BeginTransaction();
            _logger.LogInformation("Yeni transaction başlatıldı");
            return _transaction;
        }

        public void CommitTransaction()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DapperContext));
            }

            try
            {
                if (_transaction == null)
                {
                    _logger.LogWarning("Commit edilecek transaction bulunamadı");
                    return;
                }

                _transaction.Commit();
                _logger.LogInformation("Transaction commit edildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction commit edilirken hata oluştu");
                RollbackTransaction();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }
            }
        }

        public void RollbackTransaction()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DapperContext));
            }

            try
            {
                if (_transaction == null)
                {
                    _logger.LogWarning("Rollback edilecek transaction bulunamadı");
                    return;
                }

                _transaction.Rollback();
                _logger.LogInformation("Transaction rollback edildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction rollback edilirken hata oluştu");
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    _transaction?.Dispose();
                    _connection?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kaynaklar dispose edilirken hata oluştu");
                }
            }

            _transaction = null;
            _connection = null;
            _disposed = true;
        }
    }
} 