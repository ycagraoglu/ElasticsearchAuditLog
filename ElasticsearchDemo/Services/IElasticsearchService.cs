namespace ElasticsearchDemo.Services
{
    public interface IElasticsearchService
    {
        Task CheckExistsAndInsertLogAsync<T>(T logModel, string indexName) where T : class;
        Task<IEnumerable<T>> GetLogsAsync<T>(string indexName, int? entityId = null, int size = 100) where T : class;
    }
} 