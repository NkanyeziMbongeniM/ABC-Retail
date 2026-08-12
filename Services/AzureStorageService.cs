using System.Text;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using ABC.Retail.Models;

namespace ABC.Retail.Services;

/// <summary>
/// Single application service responsible for the four Azure Storage services required by Project 1.
/// Tables = structured customer/product/order data; Blobs = media; Queue = asynchronous work;
/// Azure Files = application logs.
/// </summary>
public sealed class AzureStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureStorageService> _logger;
    private readonly string _connectionString;

    private readonly string _customerTableName;
    private readonly string _productTableName;
    private readonly string _orderTableName;
    private readonly string _blobContainerName;
    private readonly string _queueName;
    private readonly string _fileShareName;

    private TableServiceClient? _tables;
    private BlobServiceClient? _blobs;
    private QueueServiceClient? _queues;
    private ShareServiceClient? _files;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);
    public string StorageAccountLabel { get; private set; } = "Not connected";

    public AzureStorageService(IConfiguration configuration, ILogger<AzureStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("AzureStorage")
            ?? configuration["AzureStorage:ConnectionString"]
            ?? string.Empty;

        _customerTableName = configuration["AzureStorage:CustomerTable"] ?? "Customers";
        _productTableName = configuration["AzureStorage:ProductTable"] ?? "Products";
        _orderTableName = configuration["AzureStorage:OrderTable"] ?? "Orders";
        _blobContainerName = configuration["AzureStorage:BlobContainer"] ?? "product-media";
        _queueName = configuration["AzureStorage:QueueName"] ?? "order-processing";
        _fileShareName = configuration["AzureStorage:FileShare"] ?? "app-logs";
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Azure Storage connection string is not configured.");
            return;
        }

        try
        {
            _tables = new TableServiceClient(_connectionString);
            _blobs = new BlobServiceClient(_connectionString);
            _queues = new QueueServiceClient(_connectionString);
            _files = new ShareServiceClient(_connectionString);

            await CustomerTable.CreateIfNotExistsAsync(cancellationToken);
            await ProductTable.CreateIfNotExistsAsync(cancellationToken);
            await OrderTable.CreateIfNotExistsAsync(cancellationToken);
            await BlobContainer.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            await Queue.CreateIfNotExistsAsync(metadata: null, cancellationToken: cancellationToken);
            await FileShare.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            await SeedMissingDemoDataAsync(cancellationToken);
            await WriteLogAsync("Application startup: Azure Storage resources verified.", cancellationToken);

            StorageAccountLabel = GetStorageAccountName(_connectionString) ?? "Azure Storage";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Storage initialization failed.");
            StorageAccountLabel = "Connection failed";
        }
    }

    private TableClient CustomerTable => _tables!.GetTableClient(_customerTableName);
    private TableClient ProductTable => _tables!.GetTableClient(_productTableName);
    private TableClient OrderTable => _tables!.GetTableClient(_orderTableName);
    private BlobContainerClient BlobContainer => _blobs!.GetBlobContainerClient(_blobContainerName);
    private QueueClient Queue => _queues!.GetQueueClient(_queueName);
    private ShareClient FileShare => _files!.GetShareClient(_fileShareName);

    private void EnsureConfigured()
    {
        if (!IsConfigured || _tables is null || _blobs is null || _queues is null || _files is null)
            throw new InvalidOperationException("Azure Storage is not configured. Add ConnectionStrings:AzureStorage in appsettings.json for local testing or App Service Configuration for deployment.");
    }

    public async Task<DashboardVm> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new DashboardVm
            {
                StorageConnected = false,
                StorageMessage = "Storage connection string is missing. Configure Azure Storage before using the data pages."
            };
        }

        try
        {
            var customers = await GetCustomersAsync(cancellationToken);
            var products = await GetProductsAsync(cancellationToken);
            var orders = await GetOrdersAsync(cancellationToken);
            var blobs = await GetBlobsAsync(cancellationToken);
            var queueMessages = await PeekQueueAsync(32, cancellationToken);
            var files = await GetFilesAsync(cancellationToken);

            return new DashboardVm
            {
                StorageConnected = true,
                StorageMessage = "Connected and ready",
                StorageAccount = StorageAccountLabel,
                Customers = customers.Count,
                Products = products.Count,
                Orders = orders.Count,
                Blobs = blobs.Count,
                QueueMessages = queueMessages.Count,
                LogFiles = files.Count,
                CustomerTable = _customerTableName,
                ProductTable = _productTableName,
                OrderTable = _orderTableName,
                BlobContainer = _blobContainerName,
                QueueName = _queueName,
                FileShare = _fileShareName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard storage read failed.");
            return new DashboardVm { StorageConnected = false, StorageMessage = ex.Message };
        }
    }

    public async Task<List<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var result = new List<Customer>();
        await foreach (var entity in CustomerTable.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
        {
            result.Add(new Customer
            {
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey,
                Name = entity.GetString("Name") ?? "",
                Email = entity.GetString("Email") ?? "",
                Phone = entity.GetString("Phone") ?? "",
                City = entity.GetString("City") ?? ""
            });
        }
        return result.OrderBy(x => x.Name).ToList();
    }

    public async Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var entity = new TableEntity(customer.PartitionKey, customer.RowKey)
        {
            ["Name"] = customer.Name.Trim(),
            ["Email"] = customer.Email.Trim(),
            ["Phone"] = customer.Phone.Trim(),
            ["City"] = customer.City.Trim(),
            ["CreatedUtc"] = DateTime.UtcNow
        };
        await CustomerTable.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken: cancellationToken);
        await WriteLogAsync($"Customer added: {customer.Name}", cancellationToken);
    }

    public async Task DeleteCustomerAsync(string rowKey, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        await CustomerTable.DeleteEntityAsync("Customer", rowKey, cancellationToken: cancellationToken);
        await WriteLogAsync($"Customer deleted: {rowKey}", cancellationToken);
    }

    public async Task<List<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var result = new List<Product>();
        await foreach (var entity in ProductTable.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
        {
            result.Add(new Product
            {
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey,
                Name = entity.GetString("Name") ?? "",
                Category = entity.GetString("Category") ?? "",
                Price = Convert.ToDecimal(entity.GetDouble("Price") ?? 0d),
                StockQuantity = entity.GetInt32("StockQuantity") ?? 0,
                Description = entity.GetString("Description") ?? ""
            });
        }
        return result.OrderBy(x => x.Name).ToList();
    }

    public async Task AddProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var entity = new TableEntity(product.PartitionKey, product.RowKey)
        {
            ["Name"] = product.Name.Trim(),
            ["Category"] = product.Category.Trim(),
            ["Price"] = (double)product.Price,
            ["StockQuantity"] = product.StockQuantity,
            ["Description"] = product.Description?.Trim() ?? "",
            ["CreatedUtc"] = DateTime.UtcNow
        };
        await ProductTable.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken: cancellationToken);
        await WriteLogAsync($"Product added: {product.Name}; stock={product.StockQuantity}; price=R{product.Price:N2}", cancellationToken);
    }

    public async Task DeleteProductAsync(string rowKey, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        await ProductTable.DeleteEntityAsync("Product", rowKey, cancellationToken: cancellationToken);
        await WriteLogAsync($"Product deleted: {rowKey}", cancellationToken);
    }

    public async Task<List<Order>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var result = new List<Order>();
        await foreach (var entity in OrderTable.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
        {
            result.Add(new Order
            {
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey,
                OrderNumber = entity.GetString("OrderNumber") ?? "",
                CustomerName = entity.GetString("CustomerName") ?? "",
                ProductName = entity.GetString("ProductName") ?? "",
                Quantity = entity.GetInt32("Quantity") ?? 0,
                UnitPrice = Convert.ToDecimal(entity.GetDouble("UnitPrice") ?? 0d),
                Total = Convert.ToDecimal(entity.GetDouble("Total") ?? 0d),
                Status = entity.GetString("Status") ?? "Queued",
                CreatedUtc = entity.GetDateTime("CreatedUtc") ?? DateTime.UtcNow
            });
        }
        return result.OrderByDescending(x => x.CreatedUtc).ToList();
    }

    public async Task AddOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var entity = new TableEntity(order.PartitionKey, order.RowKey)
        {
            ["OrderNumber"] = order.OrderNumber,
            ["CustomerName"] = order.CustomerName.Trim(),
            ["ProductName"] = order.ProductName.Trim(),
            ["Quantity"] = order.Quantity,
            ["UnitPrice"] = (double)order.UnitPrice,
            ["Total"] = (double)order.Total,
            ["Status"] = order.Status,
            ["CreatedUtc"] = order.CreatedUtc
        };

        await OrderTable.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken: cancellationToken);

        var processMessage = new
        {
            Type = "ProcessOrder",
            order.OrderNumber,
            order.CustomerName,
            order.ProductName,
            order.Quantity,
            order.Total,
            CreatedUtc = order.CreatedUtc
        };
        var inventoryMessage = new
        {
            Type = "UpdateInventory",
            order.OrderNumber,
            order.ProductName,
            QuantityToDeduct = order.Quantity
        };

        await Queue.SendMessageAsync(JsonSerializer.Serialize(processMessage), cancellationToken: cancellationToken);
        await Queue.SendMessageAsync(JsonSerializer.Serialize(inventoryMessage), cancellationToken: cancellationToken);
        await WriteLogAsync($"Order queued: {order.OrderNumber}; customer={order.CustomerName}; product={order.ProductName}; quantity={order.Quantity}", cancellationToken);
    }

    public async Task<List<BlobItemVm>> GetBlobsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var result = new List<BlobItemVm>();
        await foreach (var item in BlobContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix: null, cancellationToken: cancellationToken))
        {
            result.Add(new BlobItemVm(item.Name, item.Properties.ContentLength, item.Properties.ContentType ?? GuessContentType(item.Name), item.Properties.LastModified));
        }
        return result.OrderBy(x => x.Name).ToList();
    }

    public async Task UploadBlobAsync(Stream stream, string originalName, string contentType, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var safeName = Path.GetFileName(originalName);
        var blobName = $"uploads/{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}-{safeName}";
        var blob = BlobContainer.GetBlobClient(blobName);
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = string.IsNullOrWhiteSpace(contentType) ? GuessContentType(safeName) : contentType }
        }, cancellationToken);
        await WriteLogAsync($"Blob uploaded: {blobName}", cancellationToken);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadBlobAsync(string name, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var cleanName = name.Replace('\\', '/').TrimStart('/');
        if (cleanName.Contains("..", StringComparison.Ordinal)) throw new ArgumentException("Invalid blob name.");
        var blob = BlobContainer.GetBlobClient(cleanName);
        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return (response.Value.Content, response.Value.Details.ContentType ?? GuessContentType(cleanName), Path.GetFileName(cleanName));
    }

    public async Task DeleteBlobAsync(string name, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        await BlobContainer.DeleteBlobIfExistsAsync(name, cancellationToken: cancellationToken);
        await WriteLogAsync($"Blob deleted: {name}", cancellationToken);
    }

    public async Task<List<QueueMessageVm>> PeekQueueAsync(int count = 32, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var response = await Queue.PeekMessagesAsync(Math.Clamp(count, 1, 32), cancellationToken);
        return response.Value.Select(x => new QueueMessageVm(x.MessageId, x.Body.ToString(), x.InsertedOn, x.ExpiresOn)).ToList();
    }

    public async Task SendQueueMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        await Queue.SendMessageAsync(message, cancellationToken: cancellationToken);
        await WriteLogAsync($"Queue message added: {message}", cancellationToken);
    }

    public async Task<List<FileItemVm>> GetFilesAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var directory = FileShare.GetRootDirectoryClient();
        var result = new List<FileItemVm>();
        await foreach (var item in directory.GetFilesAndDirectoriesAsync(cancellationToken: cancellationToken))
        {
            if (item.IsDirectory) continue;
            var file = directory.GetFileClient(item.Name);
            var properties = await file.GetPropertiesAsync(cancellationToken: cancellationToken);
            result.Add(new FileItemVm(item.Name, properties.Value.ContentLength, properties.Value.LastModified));
        }
        return result.OrderByDescending(x => x.LastModified).ToList();
    }

    public async Task WriteLogAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _files is null) return;
        try
        {
            var directory = FileShare.GetRootDirectoryClient();
            var fileName = $"application-{DateTime.UtcNow:yyyy-MM-dd}.log";
            var file = directory.GetFileClient(fileName);

            string existing = string.Empty;
            try
            {
                var download = await file.DownloadAsync(cancellationToken: cancellationToken);
                using var reader = new StreamReader(download.Value.Content);
                existing = await reader.ReadToEndAsync(cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 404) { }

            var bytes = Encoding.UTF8.GetBytes(existing + $"{DateTime.UtcNow:O} | {message}{Environment.NewLine}");
            using var content = new MemoryStream(bytes, writable: false);
            await file.CreateAsync(bytes.Length, cancellationToken: cancellationToken);
            await file.UploadRangeAsync(new Azure.HttpRange(0, bytes.Length), content, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to write Azure Files log.");
        }
    }

    public async Task<(Stream Stream, string FileName)> DownloadFileAsync(string name, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var cleanName = Path.GetFileName(name);
        var file = FileShare.GetRootDirectoryClient().GetFileClient(cleanName);
        var response = await file.DownloadAsync(cancellationToken: cancellationToken);
        return (response.Value.Content, cleanName);
    }

    private async Task SeedMissingDemoDataAsync(CancellationToken cancellationToken)
    {
        // Seed each resource independently. This makes the setup self-healing if a student
        // accidentally deletes only one storage service after the first application run.
        await SeedCustomersAsync(cancellationToken);
        await SeedProductsAsync(cancellationToken);
        await SeedOrdersAsync(cancellationToken);
        await SeedQueueAsync(cancellationToken);
        await SeedBlobsAsync(cancellationToken);
        await SeedFilesAsync(cancellationToken);
    }

    private async Task SeedCustomersAsync(CancellationToken cancellationToken)
    {
        var customers = new[]
        {
            ("seed-customer-1", "Thabo Mokoena", "thabo@example.com", "011 555 0101", "Johannesburg"),
            ("seed-customer-2", "Lerato Dlamini", "lerato@example.com", "012 555 0102", "Pretoria"),
            ("seed-customer-3", "Aiden Jacobs", "aiden@example.com", "021 555 0103", "Cape Town"),
            ("seed-customer-4", "Mia Naidoo", "mia@example.com", "031 555 0104", "Durban"),
            ("seed-customer-5", "Sipho Ndlovu", "sipho@example.com", "041 555 0105", "Gqeberha")
        };
        foreach (var c in customers)
        {
            if ((await CustomerTable.GetEntityIfExistsAsync<TableEntity>("Customer", c.Item1, cancellationToken: cancellationToken)).HasValue) continue;
            await CustomerTable.AddEntityAsync(new TableEntity("Customer", c.Item1)
            {
                ["Name"] = c.Item2, ["Email"] = c.Item3, ["Phone"] = c.Item4, ["City"] = c.Item5,
                ["CreatedUtc"] = DateTime.UtcNow
            }, cancellationToken: cancellationToken);
        }
    }

    private async Task SeedProductsAsync(CancellationToken cancellationToken)
    {
        var products = new[]
        {
            ("seed-product-1", "Wireless Keyboard", "Accessories", 699.99, 42, "Compact wireless keyboard for office use."),
            ("seed-product-2", "USB-C Hub", "Accessories", 499.99, 35, "Multi-port USB-C hub for laptops."),
            ("seed-product-3", "27-inch Monitor", "Displays", 3999.99, 18, "27-inch Full HD business monitor."),
            ("seed-product-4", "Laptop Stand", "Office", 799.99, 26, "Adjustable aluminium laptop stand."),
            ("seed-product-5", "1080p Webcam", "Accessories", 1199.99, 31, "Full HD webcam for meetings and training.")
        };
        foreach (var p in products)
        {
            if ((await ProductTable.GetEntityIfExistsAsync<TableEntity>("Product", p.Item1, cancellationToken: cancellationToken)).HasValue) continue;
            await ProductTable.AddEntityAsync(new TableEntity("Product", p.Item1)
            {
                ["Name"] = p.Item2, ["Category"] = p.Item3, ["Price"] = p.Item4, ["StockQuantity"] = p.Item5,
                ["Description"] = p.Item6, ["CreatedUtc"] = DateTime.UtcNow
            }, cancellationToken: cancellationToken);
        }
    }

    private async Task SeedOrdersAsync(CancellationToken cancellationToken)
    {
        var orders = new[]
        {
            ("seed-order-1", "ORD-10001", "Thabo Mokoena", "Wireless Keyboard", 1, 699.99, 699.99),
            ("seed-order-2", "ORD-10002", "Lerato Dlamini", "USB-C Hub", 2, 499.99, 999.98),
            ("seed-order-3", "ORD-10003", "Aiden Jacobs", "27-inch Monitor", 1, 3999.99, 3999.99),
            ("seed-order-4", "ORD-10004", "Mia Naidoo", "Laptop Stand", 1, 799.99, 799.99),
            ("seed-order-5", "ORD-10005", "Sipho Ndlovu", "1080p Webcam", 2, 1199.99, 2399.98)
        };
        foreach (var o in orders)
        {
            if ((await OrderTable.GetEntityIfExistsAsync<TableEntity>("Order", o.Item1, cancellationToken: cancellationToken)).HasValue) continue;
            await OrderTable.AddEntityAsync(new TableEntity("Order", o.Item1)
            {
                ["OrderNumber"] = o.Item2, ["CustomerName"] = o.Item3, ["ProductName"] = o.Item4,
                ["Quantity"] = o.Item5, ["UnitPrice"] = o.Item6, ["Total"] = o.Item7, ["Status"] = "Queued",
                ["CreatedUtc"] = DateTime.UtcNow.AddMinutes(-o.Item5 * 10)
            }, cancellationToken: cancellationToken);
        }
    }

    private async Task SeedQueueAsync(CancellationToken cancellationToken)
    {
        var messages = await PeekQueueAsync(32, cancellationToken);
        if (messages.Count >= 5) return;
        for (var i = messages.Count + 1; i <= 5; i++)
        {
            await Queue.SendMessageAsync(JsonSerializer.Serialize(new
            {
                Type = "DemoOrderProcessing",
                OrderNumber = $"DEMO-{i:000}",
                Message = "Processing order and updating inventory."
            }), cancellationToken: cancellationToken);
        }
    }

    private async Task SeedBlobsAsync(CancellationToken cancellationToken)
    {
        for (var i = 1; i <= 5; i++)
        {
            var name = $"demo-media/product-{i}.svg";
            var blob = BlobContainer.GetBlobClient(name);
            if (await blob.ExistsAsync(cancellationToken)) continue;
            var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='900' height='500'><rect width='100%' height='100%' fill='#111827'/><text x='50%' y='45%' text-anchor='middle' fill='white' font-family='Arial' font-size='48'>ABC Retail</text><text x='50%' y='60%' text-anchor='middle' fill='#93c5fd' font-family='Arial' font-size='30'>Product Media {i}</text></svg>";
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svg));
            await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "image/svg+xml" } }, cancellationToken);
        }
    }

    private async Task SeedFilesAsync(CancellationToken cancellationToken)
    {
        var directory = FileShare.GetRootDirectoryClient();
        for (var i = 1; i <= 5; i++)
        {
            var file = directory.GetFileClient($"demo-log-{i}.txt");
            if (await file.ExistsAsync(cancellationToken)) continue;
            var bytes = Encoding.UTF8.GetBytes($"ABC Retail demonstration log record {i}{Environment.NewLine}Timestamp: {DateTime.UtcNow:O}{Environment.NewLine}Purpose: Azure Files evidence for CLDV7112w Project 1.{Environment.NewLine}");
            using var stream = new MemoryStream(bytes, writable: false);
            await file.CreateAsync(bytes.Length, cancellationToken: cancellationToken);
            await file.UploadRangeAsync(new Azure.HttpRange(0, bytes.Length), stream, cancellationToken: cancellationToken);
        }
    }

    private static string GuessContentType(string name)
    {
        return Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".mp4" => "video/mp4",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
    }

    private static string? GetStorageAccountName(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && pair[0].Equals("AccountName", StringComparison.OrdinalIgnoreCase))
                return pair[1];
        }
        return null;
    }
}
