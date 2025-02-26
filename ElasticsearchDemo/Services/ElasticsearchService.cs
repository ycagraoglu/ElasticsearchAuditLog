using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using ElasticsearchDemo.Models;
using System.Text.Json;

namespace ElasticsearchDemo.Services
{
    public class ElasticsearchService : IElasticsearchService
    {
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<ElasticsearchService> _logger;

        public ElasticsearchService(IConfiguration configuration, ILogger<ElasticsearchService> logger)
        {
            var url = configuration.GetConnectionString("ElasticsearchConnection") ?? "http://localhost:9200";
            var settings = new ConnectionSettings(new Uri(url))
                .DisableDirectStreaming(); // Debug için response detaylarını göster

            _elasticClient = new ElasticClient(settings);
            _logger = logger;
        }

        private async Task CreateIndexIfNotExistsAsync(string indexName)
        {
            try
            {
                if (!_elasticClient.Indices.Exists(indexName).Exists)
                {
                    var createIndexResponse = await _elasticClient.Indices.CreateAsync(indexName, c => c
                        .Settings(s => s
                            .NumberOfShards(1)
                            .NumberOfReplicas(1)
                            .Setting("max_result_window", 10000)
                        )
                    );

                    if (!createIndexResponse.IsValid)
                    {
                        _logger.LogError($"Index oluşturma hatası: {createIndexResponse.DebugInformation}");
                        throw new Exception($"Elasticsearch index oluşturulamadı: {indexName}");
                    }

                    _logger.LogInformation($"Index başarıyla oluşturuldu: {indexName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Index oluşturulurken hata oluştu: {indexName}");
                throw;
            }
        }

        public async Task CheckExistsAndInsertLogAsync<T>(T logModel, string indexName) where T : class
        {
            try
            {
                await CreateIndexIfNotExistsAsync(indexName);

                var response = await _elasticClient.IndexAsync(logModel, idx => idx.Index(indexName));
                
                if (!response.IsValid)
                {
                    _logger.LogError($"Log kayıt hatası: {response.DebugInformation}");
                    throw new Exception($"Log kaydedilemedi: {indexName}");
                }

                _logger.LogInformation($"Log başarıyla kaydedildi. Index: {indexName}, DocumentId: {response.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Log kaydedilirken hata oluştu. Index: {indexName}");
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetLogsAsync<T>(string indexName, int? entityId = null, int size = 100) where T : class
        {
            try
            {
                var searchDescriptor = new SearchDescriptor<T>()
                    .Index(indexName)
                    .Size(size)
                    .Sort(s => s.Descending("updatedDate"));

                if (entityId.HasValue)
                {
                    searchDescriptor = searchDescriptor.Query(q => q
                        .Term("entityId", entityId.Value));
                }

                var response = await _elasticClient.SearchAsync<T>(searchDescriptor);

                if (!response.IsValid)
                {
                    _logger.LogError($"Log getirme hatası: {response.DebugInformation}");
                    throw new Exception($"Loglar getirilemedi: {indexName}");
                }

                return response.Documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Loglar getirilirken hata oluştu. Index: {indexName}");
                throw;
            }
        }
    }
} 