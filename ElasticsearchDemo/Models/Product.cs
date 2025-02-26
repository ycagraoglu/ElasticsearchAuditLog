using System;
using ElasticsearchDemo.Services;

namespace ElasticsearchDemo.Models
{
    public class Product : BaseModel
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
    }
} 