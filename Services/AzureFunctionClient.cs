using System.Net.Http.Json;
using System.Text.Json;
using ABC.Retail.Models;

namespace ABC.Retail.Services;

/// <summary>
/// Calls the four HTTP-triggered Azure Functions introduced in Project 2.
/// The function key is read from configuration and is never hard-coded in source control.
/// </summary>
public sealed class AzureFunctionClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _functionKey;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool IsConfigured => Uri.TryCreate(_baseUrl, UriKind.Absolute, out _);
    public string StatusMessage => IsConfigured
        ? "4 Azure Functions configured"
        : "Azure Function App URL not configured - direct storage fallback is active";

    public AzureFunctionClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _baseUrl = configuration["AzureFunctions:BaseUrl"]?.Trim() ?? string.Empty;
        _functionKey = configuration["AzureFunctions:FunctionKey"]?.Trim() ?? string.Empty;
    }

    public Task StoreCustomerAsync(Customer customer, CancellationToken cancellationToken = default) =>
        StoreEntityAsync("Customers", customer.PartitionKey, customer.RowKey, new Dictionary<string, object?>
        {
            ["Name"] = customer.Name.Trim(),
            ["Email"] = customer.Email.Trim(),
            ["Phone"] = customer.Phone.Trim(),
            ["City"] = customer.City.Trim(),
            ["CreatedUtc"] = DateTime.UtcNow
        }, cancellationToken);

    public Task StoreProductAsync(Product product, CancellationToken cancellationToken = default) =>
        StoreEntityAsync("Products", product.PartitionKey, product.RowKey, new Dictionary<string, object?>
        {
            ["Name"] = product.Name.Trim(),
            ["Category"] = product.Category.Trim(),
            ["Price"] = product.Price,
            ["StockQuantity"] = product.StockQuantity,
            ["Description"] = product.Description?.Trim() ?? "",
            ["CreatedUtc"] = DateTime.UtcNow
        }, cancellationToken);

    public Task StoreOrderAsync(Order order, CancellationToken cancellationToken = default) =>
        StoreEntityAsync("Orders", order.PartitionKey, order.RowKey, new Dictionary<string, object?>
        {
            ["OrderNumber"] = order.OrderNumber,
            ["CustomerName"] = order.CustomerName.Trim(),
            ["ProductName"] = order.ProductName.Trim(),
            ["Quantity"] = order.Quantity,
            ["UnitPrice"] = order.UnitPrice,
            ["Total"] = order.Total,
            ["Status"] = order.Status,
            ["CreatedUtc"] = order.CreatedUtc
        }, cancellationToken);

    public async Task StoreEntityAsync(
        string tableName,
        string partitionKey,
        string rowKey,
        Dictionary<string, object?> properties,
        CancellationToken cancellationToken = default)
    {
        var payload = new { tableName, partitionKey, rowKey, properties };
        await SendJsonAsync(HttpMethod.Post, "table/store", payload, cancellationToken);
    }

    public async Task WriteBlobAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        var payload = new
        {
            fileName = Path.GetFileName(fileName),
            contentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            contentBase64 = Convert.ToBase64String(memory.ToArray())
        };
        await SendJsonAsync(HttpMethod.Post, "blob/write", payload, cancellationToken);
    }

    public async Task SendQueueTransactionAsync(string message, CancellationToken cancellationToken = default)
    {
        await SendJsonAsync(HttpMethod.Post, "queue/transaction", new { message }, cancellationToken);
    }

    public async Task<List<QueueMessageVm>> PeekQueueAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "queue/transaction", content: null, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<QueuePeekResponse>(stream, JsonOptions, cancellationToken);
        return result?.Messages?.Select(x => new QueueMessageVm(x.MessageId ?? "", x.Message ?? "", x.InsertedOn, x.ExpiresOn)).ToList()
               ?? new List<QueueMessageVm>();
    }

    public async Task WriteAzureFileAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        var payload = new
        {
            fileName = Path.GetFileName(fileName),
            contentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            contentBase64 = Convert.ToBase64String(memory.ToArray())
        };
        await SendJsonAsync(HttpMethod.Post, "files/write", payload, cancellationToken);
    }

    private async Task SendJsonAsync(HttpMethod method, string route, object payload, CancellationToken cancellationToken)
    {
        using var content = JsonContent.Create(payload, options: JsonOptions);
        using var response = await SendAsync(method, route, content, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string route,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Azure Functions are not configured. Set AzureFunctions:BaseUrl.");

        var uri = new Uri(new Uri(_baseUrl.TrimEnd('/') + "/"), "api/" + route.TrimStart('/'));
        var request = new HttpRequestMessage(method, uri) { Content = content };
        if (!string.IsNullOrWhiteSpace(_functionKey))
            request.Headers.TryAddWithoutValidation("x-functions-key", _functionKey);

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var statusCode = response.StatusCode;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            response.Dispose();
            throw new InvalidOperationException($"Azure Function request failed ({(int)statusCode}): {body}");
        }
        return response;
    }

    private sealed class QueuePeekResponse
    {
        public List<QueuePeekItem>? Messages { get; set; }
    }

    private sealed class QueuePeekItem
    {
        public string? MessageId { get; set; }
        public string? Message { get; set; }
        public DateTimeOffset? InsertedOn { get; set; }
        public DateTimeOffset? ExpiresOn { get; set; }
    }
}
