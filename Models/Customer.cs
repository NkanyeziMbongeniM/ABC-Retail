using System.ComponentModel.DataAnnotations;

namespace ABC.Retail.Models;

public sealed class Customer
{
    public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
    public string PartitionKey { get; set; } = "Customer";

    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(160)] public string Email { get; set; } = "";
    [Required, StringLength(30)] public string Phone { get; set; } = "";
    [Required, StringLength(80)] public string City { get; set; } = "";
}
