using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ElasticsearchDemo.Services
{
    public class ProductSyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ProductSyncBackgroundService> _logger;

        public ProductSyncBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ProductSyncBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Ürün senkronizasyonu başlatılıyor...");

                using var scope = _serviceScopeFactory.CreateScope();
                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();

                var products = await productService.GetAllProductsAsync();
                await elasticsearchService.BulkIndexProductsAsync(products);

                _logger.LogInformation("Ürün senkronizasyonu tamamlandı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün senkronizasyonu sırasında hata oluştu");
            }
        }
    }
} 