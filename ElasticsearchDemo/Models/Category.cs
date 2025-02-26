using System;
using ElasticsearchDemo.Services;

namespace ElasticsearchDemo.Models
{
    public class Category : IBaseEntity
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public int GetId() => CategoryId;
        public void SetId(int id) => CategoryId = id;
        public void SetUpdatedDate(DateTime date) => UpdatedDate = date;
    }
} 