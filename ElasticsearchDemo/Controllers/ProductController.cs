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
        private readonly ILogger<ProductController> _logger;
        private readonly IElasticsearchService _elasticsearchService;
        private readonly IProductService _productService;
        private const string ProductLogIndex = "product_updates";

        public ProductController(
            ILogger<ProductController> logger,
            IElasticsearchService elasticsearchService,
            IProductService productService)
        {
            _logger = logger;
            _elasticsearchService = elasticsearchService;
            _productService = productService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] Product product)
        {
            try
            {
                var createdProduct = await _productService.AddProductAsync(product);
                return Ok(createdProduct);
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
                product.Id = id;
                var updatedProduct = await _productService.UpdateProductAsync(product);
                return Ok(updatedProduct);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
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
                await _productService.DeleteProductAsync(id);
                return Ok($"Ürün başarıyla silindi: {id}");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
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