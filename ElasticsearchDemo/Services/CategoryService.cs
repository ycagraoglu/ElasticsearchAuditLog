using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ElasticsearchDemo.Models;
using ElasticsearchDemo.Services.Database;
using Dapper;
using System.Data;

namespace ElasticsearchDemo.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category> CreateCategoryAsync(Category category);
        Task<Category> UpdateCategoryAsync(int id, Category category);
        Task DeleteCategoryAsync(int id);
        Task<bool> CategoryHasProductsAsync(int categoryId);
    }

    public class CategoryService : ICategoryService
    {
        private readonly IDapperContext _dapperContext;
        private readonly IElasticsearchService _elasticsearchService;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(
            IDapperContext dapperContext,
            IElasticsearchService elasticsearchService,
            ILogger<CategoryService> logger)
        {
            _dapperContext = dapperContext;
            _elasticsearchService = elasticsearchService;
            _logger = logger;
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
                    "SELECT * FROM Categories WHERE Id = @Id", 
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

                var categoryId = await _dapperContext.Connection.ExecuteScalarAsync<int>(sql, category);
                category.Id = categoryId;

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
                category.Id = id;
                return await _dapperContext.Connection.UpdateWithAuditAsync(category, _dapperContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kategori güncellenirken hata oluştu: {id}");
                throw;
            }
        }

        public async Task DeleteCategoryAsync(int id)
        {
            try
            {
                if (await CategoryHasProductsAsync(id))
                {
                    throw new InvalidOperationException($"CategoryId: {id} kategorisine bağlı ürünler var. Önce bu ürünleri silmelisiniz.");
                }

                await _dapperContext.Connection.DeleteWithAuditAsync<Category>(id, _dapperContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kategori silinirken hata oluştu: {id}");
                throw;
            }
        }

        public async Task<bool> CategoryHasProductsAsync(int categoryId)
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