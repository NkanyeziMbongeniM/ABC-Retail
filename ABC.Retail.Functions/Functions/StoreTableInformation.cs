using System.Net;
using Azure.Data.Tables;
using ABC.Retail.Functions.Models;
using ABC.Retail.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABC.Retail.Functions.Functions;

public sealed class StoreTableInformation
{
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "Customers", "Products", "Orders"
    };

    private readonly AzureStorageClients _storage;
    private readonly ILogger<StoreTableInformation> _logger;

    public StoreTableInformation(AzureStorageClients storage, ILogger<StoreTableInformation> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [Function("StoreTableInformation")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "table/store")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var input = await FunctionHttp.ReadJsonAsync<TableStoreRequest>(req, cancellationToken);
        if (input is null || !AllowedTables.Contains(input.TableName) ||
            string.IsNullOrWhiteSpace(input.PartitionKey) || string.IsNullOrWhiteSpace(input.RowKey))
        {
            return await FunctionHttp.JsonAsync(req, HttpStatusCode.BadRequest,
                new { error = "tableName must be Customers, Products or Orders and both keys are required." }, cancellationToken);
        }

        if (HasInvalidKeyCharacters(input.PartitionKey) || HasInvalidKeyCharacters(input.RowKey))
        {
            return await FunctionHttp.JsonAsync(req, HttpStatusCode.BadRequest,
                new { error = "PartitionKey/RowKey contains an invalid Azure Table key character." }, cancellationToken);
        }

        var table = _storage.GetTable(input.TableName);
        await table.CreateIfNotExistsAsync(cancellationToken);

        var entity = new TableEntity(input.PartitionKey, input.RowKey);
        foreach (var pair in input.Properties)
        {
            var converted = ConvertTableValue(pair.Value);
            if (converted is not null) entity[pair.Key] = converted;
        }
        entity["FunctionProcessedUtc"] = DateTime.UtcNow;

        await table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
        _logger.LogInformation("Stored entity {PartitionKey}/{RowKey} in {TableName}.", input.PartitionKey, input.RowKey, input.TableName);

        return await FunctionHttp.JsonAsync(req, HttpStatusCode.OK, new
        {
            success = true,
            function = "StoreTableInformation",
            table = input.TableName,
            input.PartitionKey,
            input.RowKey,
            processedUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    private static bool HasInvalidKeyCharacters(string value) =>
        value.Any(ch => ch is '/' or '\\' or '#' or '?' || char.IsControl(ch));

    private static object? ConvertTableValue(System.Text.Json.JsonElement value)
    {
        return value.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String when DateTimeOffset.TryParse(value.GetString(), out var date) => date,
            System.Text.Json.JsonValueKind.String => value.GetString(),
            System.Text.Json.JsonValueKind.Number when value.TryGetInt32(out var i) => i,
            System.Text.Json.JsonValueKind.Number when value.TryGetInt64(out var l) => l,
            System.Text.Json.JsonValueKind.Number when value.TryGetDouble(out var d) => d,
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null,
            _ => value.GetRawText()
        };
    }
}
