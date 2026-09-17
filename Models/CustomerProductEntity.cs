using Azure;
using Azure.Data.Tables;

namespace ABCRetailFunctions.Models
{
    // Same shape as the entity used in Project 1's web app (ABCRetailStorageApp),
    // so records written here land in the same CustomerProductTable.
    public class CustomerProductEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer"; // "Customer" or "Product"
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Description { get; set; }
        public double? Price { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
