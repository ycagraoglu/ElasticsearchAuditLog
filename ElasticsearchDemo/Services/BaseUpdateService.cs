using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using System.Text.RegularExpressions;
using ElasticsearchDemo.Services.Database;
using ElasticsearchDemo.Models;

namespace ElasticsearchDemo.Services
{
    public class TableNotFoundException : Exception
    {
        public TableNotFoundException(string tableName)
            : base($"Veritabanında '{tableName}' isimli tablo bulunamadı. Lütfen tablo adını ve veritabanı bağlantısını kontrol edin.")
        {
        }
    }

    public abstract class BaseUpdateService<T> where T : BaseModel
    {
        protected readonly ILogger _logger;
        protected readonly IElasticsearchService _elasticsearchService;
        protected readonly IDapperContext _dapperContext;
        private readonly string _defaultTableName;
        private readonly string _defaultLogIndexName;
        private bool _isTableChecked;

        protected BaseUpdateService(
            ILogger logger,
            IElasticsearchService elasticsearchService,
            IDapperContext dapperContext)
        {
            _logger = logger;
            _elasticsearchService = elasticsearchService;
            _dapperContext = dapperContext;

            var entityName = typeof(T).Name;
            _defaultTableName = entityName + "s";
            _defaultLogIndexName = $"{ToSnakeCase(entityName)}_updates";
            _isTableChecked = false;
        }

        private static string ToSnakeCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var result = Regex.Replace(text, "([a-z0-9])([A-Z])", "$1_$2").ToLower();
            return result;
        }

        protected virtual string UpdateSql => $@"
            UPDATE {TableName} 
            SET {string.Join(",", GetUpdateColumns())}
            WHERE Id = @Id";

        protected virtual string TableName => _defaultTableName;
        protected virtual string LogIndexName => _defaultLogIndexName;

        private IEnumerable<string> GetUpdateColumns()
        {
            var properties = typeof(T).GetProperties()
                .Where(p => !IsExcludedFromUpdate(p.Name))
                .Select(p => $"{p.Name} = @{p.Name}");

            return properties;
        }

        private bool IsExcludedFromUpdate(string propertyName)
        {
            return propertyName switch
            {
                "Id" => true,
                "CreatedDate" => true,
                _ => false
            };
        }

        private async Task EnsureTableExistsAsync()
        {
            if (_isTableChecked) return;

            var sql = @"
                SELECT COUNT(1) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = @TableName";

            var exists = await _dapperContext.Connection.ExecuteScalarAsync<int>(sql, new { TableName = TableName }) > 0;

            if (!exists)
            {
                _logger.LogError($"Tablo bulunamadı: {TableName}");
                throw new TableNotFoundException(TableName);
            }

            _isTableChecked = true;
            _logger.LogInformation($"Tablo kontrolü başarılı: {TableName}");
        }

        protected virtual async Task<T> UpdateEntityAsync(T entity)
        {
            await EnsureTableExistsAsync();

            var transaction = await _dapperContext.BeginTransactionAsync();
            try
            {
                var oldEntity = await _dapperContext.Connection.QueryFirstOrDefaultAsync<T>(
                    $"SELECT * FROM {TableName} WITH (UPDLOCK) WHERE Id = @Id",
                    new { entity.Id },
                    transaction);

                if (oldEntity == null)
                    throw new KeyNotFoundException($"{TableName} Id: {entity.Id} bulunamadı");

                entity.UpdatedDate = DateTime.UtcNow;

                await _dapperContext.Connection.ExecuteAsync(
                    UpdateSql,
                    entity,
                    transaction);

                var logModel = new
                {
                    EntityId = entity.Id,
                    ClassName = TableName,
                    Operation = "Update",
                    OldData = oldEntity,
                    NewData = entity,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "system"
                };

                await _elasticsearchService.CheckExistsAndInsertLogAsync(logModel, LogIndexName);

                _dapperContext.CommitTransaction();
                return entity;
            }
            catch
            {
                _dapperContext.RollbackTransaction();
                throw;
            }
        }
    }
} 