using System.ComponentModel.DataAnnotations;

namespace ABC.Retail.Models;

public sealed class Product
{
    public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
    public string PartitionKey { get; set; } = "Product";

    [Required, StringLength(120)] public string Name { get; set; } = "";
    [Required, StringLength(80)] public string Category { get; set; } = "";
    [Range(0, 100000000)] public decimal Price { get; set; }
    [Range(0, 100000000)] public int StockQuantity { get; set; }
    [StringLength(500)] public string Description { get; set; } = "";
}
