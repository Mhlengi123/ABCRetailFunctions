namespace ABCRetailFunctions.Models
{
    // Same shape as Project 1's order/inventory message, so messages written
    // or read here are compatible with the existing order-processing-queue.
    public class OrderMessage
    {
        public string OrderId { get; set; } = Guid.NewGuid().ToString();
        public string Action { get; set; } = string.Empty;   // e.g. "Processing order", "Inventory update"
        public string Details { get; set; } = string.Empty;  // e.g. product name, quantity, transaction note
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
