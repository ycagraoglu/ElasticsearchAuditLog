using Dapper;
using Microsoft.Extensions.Logging;
using ElasticsearchDemo.Models;
using ElasticsearchDemo.Services.Database;

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

    public class ProductService : IProductService
    {
        private readonly ILogger<ProductService> _logger;
        private readonly IDapperContext _dapperContext;

        public ProductService(
            ILogger<ProductService> logger,
            IDapperContext dapperContext)
        {
            _logger = logger;
            _dapperContext = dapperContext;
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
                return await _dapperContext.Connection.UpdateWithAuditAsync(product, _dapperContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün güncellenirken hata oluştu. Id: {product.Id}");
                throw;
            }
        }

        public async Task DeleteProductAsync(int productId)
        {
            try
            {
                await _dapperContext.Connection.DeleteWithAuditAsync<Product>(productId, _dapperContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün silinirken hata oluştu: {productId}");
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