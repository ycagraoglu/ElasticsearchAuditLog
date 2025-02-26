using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ElasticsearchDemo.Models;
using ElasticsearchDemo.Services.Database;
using System.Data;

namespace ElasticsearchDemo.Services
{
    public interface IProductService
    {
        Task<Product> AddProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task DeleteProductAsync(int productId);
        Task<Product> GetProductByIdAsync(int productId);
        Task<IEnumerable<Product>> GetAllProductsAsync();
    }

    public class ProductService : BaseUpdateService<Product>, IProductService
    {
        public ProductService(
            ILogger<ProductService> logger,
            IElasticsearchService elasticsearchService,
            IDapperContext dapperContext)
            : base(logger, elasticsearchService, dapperContext)
        {
        }

        public async Task<Product> AddProductAsync(Product product)
        {
            try
            {
                const string sql = @"
                    INSERT INTO Products (ProductName, Price, Category, CategoryId, CreatedDate)
                    VALUES (@ProductName, @Price, @Category, @CategoryId, @CreatedDate);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                var productId = await _dapperContext.Connection.QuerySingleAsync<int>(sql, product);
                product.Id = productId;

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün eklenirken hata oluştu");
                throw;
            }
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            try
            {
                return await UpdateEntityAsync(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün güncellenirken hata oluştu. Id: {product.Id}");
                throw;
            }
        }

        public async Task DeleteProductAsync(int productId)
        {
            var transaction = await _dapperContext.BeginTransactionAsync();
            try
            {
                var oldProduct = await _dapperContext.Connection.QueryFirstOrDefaultAsync<Product>(
                    "SELECT * FROM Products WHERE Id = @Id",
                    new { Id = productId },
                    transaction);

                if (oldProduct == null)
                    throw new KeyNotFoundException($"Id: {productId} bulunamadı");

                await _dapperContext.Connection.ExecuteAsync(
                    "DELETE FROM Products WHERE Id = @Id",
                    new { Id = productId },
                    transaction);

                _dapperContext.CommitTransaction();
            }
            catch
            {
                _dapperContext.RollbackTransaction();
                throw;
            }
        }

        public async Task<Product> GetProductByIdAsync(int productId)
        {
            try
            {
                const string sql = "SELECT * FROM Products WHERE Id = @Id";

                var product = await _dapperContext.Connection.QuerySingleOrDefaultAsync<Product>(
                    sql, 
                    new { Id = productId });

                if (product == null)
                {
                    _logger.LogWarning($"Ürün bulunamadı. Id: {productId}");
                    throw new KeyNotFoundException($"Id: {productId} bulunamadı");
                }

                _logger.LogInformation($"Ürün başarıyla getirildi. Id: {productId}");
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün getirilirken hata oluştu. Id: {productId}");
                throw;
            }
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            try
            {
                const string sql = "SELECT * FROM Products";
                var products = await _dapperContext.Connection.QueryAsync<Product>(sql);

                _logger.LogInformation("Tüm ürünler başarıyla getirildi");
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürünler getirilirken hata oluştu");
                throw;
            }
        }
    }
} 