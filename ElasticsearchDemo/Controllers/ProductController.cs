using Microsoft.AspNetCore.Mvc;
using ElasticsearchDemo.Models;
using ElasticsearchDemo.Services;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Text.Json;
using System.Collections.Generic;

namespace ElasticsearchDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProductController> _logger;
        private readonly IElasticsearchService _elasticsearchService;
        private const string ProductLogIndex = "product_updates";

        public ProductController(
            IConfiguration configuration,
            ILogger<ProductController> logger,
            IElasticsearchService elasticsearchService)
        {
            _configuration = configuration;
            _logger = logger;
            _elasticsearchService = elasticsearchService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] Product product)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                const string sql = @"
                    INSERT INTO Products (ProductName, Price, Category, CreatedDate) 
                    VALUES (@ProductName, @Price, @Category, @CreatedDate);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                product.CreatedDate = DateTime.UtcNow;
                var productId = await connection.ExecuteScalarAsync<int>(sql, product);
                product.ProductId = productId;

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün oluşturulurken hata oluştu");
                return StatusCode(500, "Ürün oluşturulurken bir hata oluştu");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();
                try
                {
                    // Mevcut ürünü kilit ile al
                    var oldProduct = await connection.QueryFirstOrDefaultAsync<Product>(
                        "SELECT * FROM Products WITH (UPDLOCK) WHERE ProductId = @Id",
                        new { Id = id },
                        transaction);

                    if (oldProduct == null)
                        return NotFound($"ProductId: {id} bulunamadı");

                    // Sadece DB seviyesinde kontrol yapalım
                    product.ProductId = id;
                    product.UpdatedDate = DateTime.UtcNow;

                    const string sql = @"
                        UPDATE Products 
                        SET ProductName = @ProductName, 
                            Price = @Price, 
                            Category = @Category,
                            UpdatedDate = @UpdatedDate
                        WHERE ProductId = @ProductId";

                    await connection.ExecuteAsync(
                        sql,
                        new { 
                            product.ProductId,
                            product.ProductName,
                            product.Price,
                            product.Category,
                            product.UpdatedDate
                        },
                        transaction);

                    var logModel = new
                    {
                        EntityId = id,
                        ClassName = "Products",
                        Operation = "Update",
                        OldProduct = oldProduct,
                        NewProduct = product,
                        UpdatedDate = DateTime.UtcNow,
                        UpdatedBy = "system",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                        Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                        ApplicationName = "ProductAPI"
                    };

                    await _elasticsearchService.CheckExistsAndInsertLogAsync(logModel, ProductLogIndex);

                    transaction.Commit();
                    return Ok(product);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün güncellenirken hata oluştu: {id}");
                return StatusCode(500, "Ürün güncellenirken bir hata oluştu");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                
                var oldProduct = await connection.QueryFirstOrDefaultAsync<Product>(
                    "SELECT * FROM Products WHERE ProductId = @Id", new { Id = id });

                if (oldProduct == null)
                    return NotFound($"ProductId: {id} bulunamadı");

                await connection.ExecuteAsync(
                    "DELETE FROM Products WHERE ProductId = @Id", new { Id = id });

                return Ok($"Ürün başarıyla silindi: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün silinirken hata oluştu: {id}");
                return StatusCode(500, "Ürün silinirken bir hata oluştu");
            }
        }

        [HttpGet("{id}/update-history")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetProductUpdateHistory(int id)
        {
            try
            {
                var logs = await _elasticsearchService.GetLogsAsync<dynamic>(ProductLogIndex, id);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ürün güncelleme geçmişi getirilirken hata oluştu: {id}");
                return StatusCode(500, "Ürün güncelleme geçmişi getirilirken bir hata oluştu");
            }
        }
    }
}