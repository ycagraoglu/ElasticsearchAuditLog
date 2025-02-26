using System;
using System.Text.Json;

namespace ElasticsearchDemo.Models
{
    public class ProductAudit
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int ProductId { get; set; }
        public string OldData { get; set; } // JSON formatında eski veri
        public string NewData { get; set; } // JSON formatında yeni veri
        public string UpdatedUserId { get; set; } = "system"; // Şimdilik sabit bir değer
        public DateTime UpdatedDate { get; set; }
        public string ChangeType { get; set; } // "Update", "Create", "Delete"
    }
} 