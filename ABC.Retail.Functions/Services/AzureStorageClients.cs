using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;

namespace ABC.Retail.Functions.Services;

public sealed class AzureStorageClients
{
    private readonly string _connectionString;
    private readonly IConfiguration _configuration;

    public AzureStorageClients(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = configuration["AzureStorage"]
            ?? configuration.GetConnectionString("AzureStorage")
            ?? configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("AzureStorage app setting is not configured.");
    }

    public string BlobContainerName => _configuration["BlobContainer"] ?? "product-media";
    public string QueueName => _configuration["QueueName"] ?? "order-processing";
    public string FileShareName => _configuration["FileShare"] ?? "app-logs";

    public TableClient GetTable(string tableName) => new TableServiceClient(_connectionString).GetTableClient(tableName);
    public BlobServiceClient Blobs => new(_connectionString);
    public QueueServiceClient Queues => new(_connectionString);
    public ShareServiceClient Files => new(_connectionString);
}
