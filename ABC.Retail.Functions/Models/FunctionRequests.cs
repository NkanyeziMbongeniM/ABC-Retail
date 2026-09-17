using System.Text.Json;

namespace ABC.Retail.Functions.Models;

public sealed class TableStoreRequest
{
    public string TableName { get; set; } = "";
    public string PartitionKey { get; set; } = "";
    public string RowKey { get; set; } = "";
    public Dictionary<string, JsonElement> Properties { get; set; } = new();
}

public sealed class BinaryWriteRequest
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public string ContentBase64 { get; set; } = "";
}

public sealed class QueueWriteRequest
{
    public string Message { get; set; } = "";
}
