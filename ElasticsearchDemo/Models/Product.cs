using System;
using ElasticsearchDemo.Services;

namespace ElasticsearchDemo.Models
{
    public class Product : IBaseEntity
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public int GetId() => ProductId;
        public void SetId(int id) => ProductId = id;
        public void SetUpdatedDate(DateTime date) => UpdatedDate = date;
    }
} 