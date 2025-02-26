using System;
using ElasticsearchDemo.Services;
using ElasticsearchDemo.Services.Database;

namespace ElasticsearchDemo.Models
{
    public class Category : BaseModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
} 