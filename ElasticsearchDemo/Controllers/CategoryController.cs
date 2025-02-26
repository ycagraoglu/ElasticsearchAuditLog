using Microsoft.AspNetCore.Mvc;
using ElasticsearchDemo.Models;
using ElasticsearchDemo.Services;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Text.Json;

namespace ElasticsearchDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ILogger<CategoryController> _logger;
        private readonly ICategoryService _categoryService;
        private readonly IElasticsearchService _elasticsearchService;
        private const string CategoryLogIndex = "category_updates";

        public CategoryController(
            ILogger<CategoryController> logger,
            ICategoryService categoryService,
            IElasticsearchService elasticsearchService)
        {
            _logger = logger;
            _categoryService = categoryService;
            _elasticsearchService = elasticsearchService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategoriler getirilirken hata oluştu");
                return StatusCode(500, "Kategoriler getirilirken bir hata oluştu");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                    return NotFound($"CategoryId: {id} bulunamadı");

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kategori getirilirken hata oluştu: {id}");
                return StatusCode(500, "Kategori getirilirken bir hata oluştu");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            try
            {
                var createdCategory = await _categoryService.CreateCategoryAsync(category);
                return Ok(createdCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategori oluşturulurken hata oluştu");
                return StatusCode(500, "Kategori oluşturulurken bir hata oluştu");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
        {
            try
            {
                var updatedCategory = await _categoryService.UpdateCategoryAsync(id, category);
                return Ok(updatedCategory);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kategori güncellenirken hata oluştu: {id}");
                return StatusCode(500, "Kategori güncellenirken bir hata oluştu");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                await _categoryService.DeleteCategoryAsync(id);
                return Ok($"Kategori başarıyla silindi: {id}");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kategori silinirken hata oluştu: {id}");
                return StatusCode(500, "Kategori silinirken bir hata oluştu");
            }
        }

        [HttpGet("{id}/update-history")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetCategoryUpdateHistory(int id)
        {
            try
            {
                var logs = await _elasticsearchService.GetLogsAsync<dynamic>(CategoryLogIndex, id);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kategori güncelleme geçmişi getirilirken hata oluştu: {id}");
                return StatusCode(500, "Kategori güncelleme geçmişi getirilirken bir hata oluştu");
            }
        }
    }
} 