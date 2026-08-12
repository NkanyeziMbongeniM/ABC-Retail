using System.ComponentModel.DataAnnotations;

namespace ABC.Retail.Models;

public sealed class Order
{
    public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
    public string PartitionKey { get; set; } = "Order";
    public string OrderNumber { get; set; } = "";

    [Required, StringLength(100)] public string CustomerName { get; set; } = "";
    [Required, StringLength(120)] public string ProductName { get; set; } = "";
    [Range(1, 100000)] public int Quantity { get; set; } = 1;
    [Range(0, 100000000)] public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Queued";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
