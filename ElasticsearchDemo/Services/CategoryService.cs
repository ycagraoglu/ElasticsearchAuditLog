using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ElasticsearchDemo.Models;
using ElasticsearchDemo.Services.Database;
using Dapper;

namespace ElasticsearchDemo.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category> CreateCategoryAsync(Category category);
        Task<Category> UpdateCategoryAsync(int id, Category category);
        Task DeleteCategoryAsync(int id);
        Task<bool> HasProductsAsync(int categoryId);
    }

    public class CategoryService : BaseUpdateService<Category>, ICategoryService
    {
        public CategoryService(
            ILogger<CategoryService> logger,
            IElasticsearchService elasticsearchService,
            IDapperContext dapperContext)
            : base(logger, elasticsearchService, dapperContext)
        {
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            try
            {
                return await _dapperContext.Connection.QueryAsync<Category>("SELECT * FROM Categories");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategoriler getirilirken hata oluştu");
                throw;
            }
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            try
            {
                return await _dapperContext.Connection.QueryFirstOrDefaultAsync<Category>(
                    "SELECT * FROM Categories WHERE CategoryId = @Id", 
                    new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kategori getirilirken hata oluştu: {id}");
                throw;
            }
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            try
            {
                const string sql = @"
                    INSERT INTO Categories (CategoryName, Description, CreatedDate) 
                    VALUES (@CategoryName, @Description, @CreatedDate);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                category.CreatedDate = DateTime.UtcNow;
                var categoryId = await _dapperContext.Connection.ExecuteScalarAsync<int>(sql, category);
                category.CategoryId = categoryId;

                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategori oluşturulurken hata oluştu");
                throw;
            }
        }

        public async Task<Category> UpdateCategoryAsync(int id, Category category)
        {
            try
            {
                category.SetId(id);
                return await UpdateEntityAsync(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kategori güncellenirken hata oluştu: {id}");
                throw;
            }
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var transaction = await _dapperContext.BeginTransactionAsync();
            try
            {
                if (await HasProductsAsync(id))
                {
                    throw new InvalidOperationException($"CategoryId: {id} kategorisine bağlı ürünler var. Önce bu ürünleri silmelisiniz.");
                }

                var oldCategory = await _dapperContext.Connection.QueryFirstOrDefaultAsync<Category>(
                    "SELECT * FROM Categories WHERE CategoryId = @Id",
                    new { Id = id },
                    transaction);

                if (oldCategory == null)
                    throw new KeyNotFoundException($"CategoryId: {id} bulunamadı");

                await _dapperContext.Connection.ExecuteAsync(
                    "DELETE FROM Categories WHERE CategoryId = @Id",
                    new { Id = id },
                    transaction);

                _dapperContext.CommitTransaction();
            }
            catch
            {
                _dapperContext.RollbackTransaction();
                throw;
            }
        }

        public async Task<bool> HasProductsAsync(int categoryId)
        {
            try
            {
                return await _dapperContext.Connection.ExecuteScalarAsync<bool>(
                    "SELECT CASE WHEN EXISTS (SELECT 1 FROM Products WHERE CategoryId = @Id) THEN 1 ELSE 0 END",
                    new { Id = categoryId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kategori ürün kontrolü yapılırken hata oluştu: {categoryId}");
                throw;
            }
        }
    }
} 