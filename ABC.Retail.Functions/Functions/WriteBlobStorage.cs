using System.Net;
using Azure.Storage.Blobs.Models;
using ABC.Retail.Functions.Models;
using ABC.Retail.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABC.Retail.Functions.Functions;

public sealed class WriteBlobStorage
{
    private readonly AzureStorageClients _storage;
    private readonly ILogger<WriteBlobStorage> _logger;

    public WriteBlobStorage(AzureStorageClients storage, ILogger<WriteBlobStorage> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [Function("WriteBlobStorage")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blob/write")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var input = await FunctionHttp.ReadJsonAsync<BinaryWriteRequest>(req, cancellationToken);
        if (input is null || string.IsNullOrWhiteSpace(input.FileName) || string.IsNullOrWhiteSpace(input.ContentBase64))
        {
            return await FunctionHttp.JsonAsync(req, HttpStatusCode.BadRequest,
                new { error = "fileName and contentBase64 are required." }, cancellationToken);
        }

        byte[] bytes;
        try { bytes = Convert.FromBase64String(input.ContentBase64); }
        catch (FormatException)
        {
            return await FunctionHttp.JsonAsync(req, HttpStatusCode.BadRequest,
                new { error = "contentBase64 is not valid Base64." }, cancellationToken);
        }

        var safeName = Path.GetFileName(input.FileName);
        if (string.IsNullOrWhiteSpace(safeName) || bytes.Length == 0)
        {
            return await FunctionHttp.JsonAsync(req, HttpStatusCode.BadRequest,
                new { error = "The file name or content is empty." }, cancellationToken);
        }

        var container = _storage.Blobs.GetBlobContainerClient(_storage.BlobContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var blobName = $"function-uploads/{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}-{safeName}";
        var blob = container.GetBlobClient(blobName);
        using var stream = new MemoryStream(bytes, writable: false);
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(input.ContentType) ? "application/octet-stream" : input.ContentType
            }
        }, cancellationToken);

        _logger.LogInformation("Function uploaded blob {BlobName} ({Length} bytes).", blobName, bytes.Length);
        return await FunctionHttp.JsonAsync(req, HttpStatusCode.OK, new
        {
            success = true,
            function = "WriteBlobStorage",
            container = _storage.BlobContainerName,
            blobName,
            bytes = bytes.Length,
            processedUtc = DateTime.UtcNow
        }, cancellationToken);
    }
}
