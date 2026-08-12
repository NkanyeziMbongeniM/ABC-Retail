namespace ABC.Retail.Models;

public sealed record BlobItemVm(string Name, long? Size, string ContentType, DateTimeOffset? LastModified);
public sealed record QueueMessageVm(string MessageId, string Text, DateTimeOffset? InsertionTime, DateTimeOffset? ExpirationTime);
public sealed record FileItemVm(string Name, long Size, DateTimeOffset? LastModified);

public sealed class DashboardVm
{
    public int Customers { get; set; }
    public int Products { get; set; }
    public int Orders { get; set; }
    public int Blobs { get; set; }
    public int QueueMessages { get; set; }
    public int LogFiles { get; set; }
    public bool StorageConnected { get; set; }
    public string StorageMessage { get; set; } = "";
    public string StorageAccount { get; set; } = "";
    public string CustomerTable { get; set; } = "Customers";
    public string ProductTable { get; set; } = "Products";
    public string OrderTable { get; set; } = "Orders";
    public string BlobContainer { get; set; } = "product-media";
    public string QueueName { get; set; } = "order-processing";
    public string FileShare { get; set; } = "app-logs";
}
