using System.Data;
using Dapper;
using ElasticsearchDemo.Models;
using ElasticsearchDemo.Services;

namespace ElasticsearchDemo.Services.Database
{
    public static class DapperExtensions
    {
        public static async Task<T> UpdateWithAuditAsync<T>(
            this IDapperContext dapperContext,
            T entity) where T : BaseModel
        {
            ArgumentNullException.ThrowIfNull(dapperContext);
            ArgumentNullException.ThrowIfNull(entity);

            var transaction = await dapperContext.BeginTransactionAsync();
            try
            {
                // Tablo adını entity'den al
                var tableName = typeof(T).Name + "s";  // Categories, Products vs.

                // Mevcut veriyi al
                var oldEntity = await dapperContext.Connection.QueryFirstOrDefaultAsync<T>(
                    $"SELECT * FROM {tableName} WITH (UPDLOCK) WHERE Id = @Id",
                    new { entity.Id },
                    transaction);

                if (oldEntity == null)
                    throw new KeyNotFoundException($"{tableName} Id: {entity.Id} bulunamadı");

                // Update işlemini yap
                entity.UpdatedDate = DateTime.UtcNow;
                await dapperContext.Connection.ExecuteAsync(
                    GetUpdateSql<T>(tableName),
                    entity,
                    transaction);

                // Audit log
                var logModel = new
                {
                    EntityId = entity.Id,
                    ClassName = tableName,
                    Operation = "Update",
                    OldData = oldEntity,
                    NewData = entity,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "system"
                };

                await dapperContext.ElasticsearchService.CheckExistsAndInsertLogAsync(
                    logModel, 
                    $"{tableName.ToLower()}_updates");

                dapperContext.CommitTransaction();
                return entity;
            }
            catch
            {
                dapperContext.RollbackTransaction();
                throw;
            }
        }

        public static async Task DeleteWithAuditAsync<T>(
            this IDapperContext dapperContext,
            int id) where T : BaseModel
        {
            ArgumentNullException.ThrowIfNull(dapperContext);

            var transaction = await dapperContext.BeginTransactionAsync();
            try
            {
                var tableName = typeof(T).Name + "s";

                // Mevcut veriyi al
                var oldEntity = await dapperContext.Connection.QueryFirstOrDefaultAsync<T>(
                    $"SELECT * FROM {tableName} WITH (UPDLOCK) WHERE Id = @Id",
                    new { Id = id },
                    transaction);

                if (oldEntity == null)
                    throw new KeyNotFoundException($"{tableName} Id: {id} bulunamadı");

                // Delete işlemini yap
                await dapperContext.Connection.ExecuteAsync(
                    $"DELETE FROM {tableName} WHERE Id = @Id",
                    new { Id = id },
                    transaction);

                // Audit log
                var logModel = new
                {
                    EntityId = id,
                    ClassName = tableName,
                    Operation = "Delete",
                    OldData = oldEntity,
                    NewData = (object?)null,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "system"
                };

                await dapperContext.ElasticsearchService.CheckExistsAndInsertLogAsync(
                    logModel, 
                    $"{tableName.ToLower()}_updates");

                dapperContext.CommitTransaction();
            }
            catch
            {
                dapperContext.RollbackTransaction();
                throw;
            }
        }

        private static string GetUpdateSql<T>(string tableName) where T : BaseModel
        {
            ArgumentNullException.ThrowIfNull(tableName);

            var properties = typeof(T).GetProperties()
                .Where(p => !IsExcludedFromUpdate(p.Name))
                .Select(p => $"{p.Name} = @{p.Name}");

            return $@"
                UPDATE {tableName} 
                SET {string.Join(",", properties)}
                WHERE Id = @Id";
        }

        private static bool IsExcludedFromUpdate(string propertyName)
        {
            ArgumentNullException.ThrowIfNull(propertyName);

            return propertyName switch
            {
                "Id" => true,
                "CreatedDate" => true,
                _ => false
            };
        }
    }
} 