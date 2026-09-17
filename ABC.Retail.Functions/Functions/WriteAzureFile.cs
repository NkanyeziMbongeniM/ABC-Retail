using System.Net;
using Azure;
using ABC.Retail.Functions.Models;
using ABC.Retail.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABC.Retail.Functions.Functions;

public sealed class WriteAzureFile
{
    private readonly AzureStorageClients _storage;
    private readonly ILogger<WriteAzureFile> _logger;

    public WriteAzureFile(AzureStorageClients storage, ILogger<WriteAzureFile> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [Function("WriteAzureFile")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "files/write")] HttpRequestData req,
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

        var share = _storage.Files.GetShareClient(_storage.FileShareName);
        await share.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var directory = share.GetRootDirectoryClient();
        var file = directory.GetFileClient(safeName);

        await file.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        await file.CreateAsync(bytes.LongLength, cancellationToken: cancellationToken);
        using var stream = new MemoryStream(bytes, writable: false);
        await file.UploadRangeAsync(new HttpRange(0, bytes.LongLength), stream, cancellationToken: cancellationToken);

        _logger.LogInformation("Function wrote Azure File {FileName} ({Length} bytes).", safeName, bytes.Length);
        return await FunctionHttp.JsonAsync(req, HttpStatusCode.OK, new
        {
            success = true,
            function = "WriteAzureFile",
            share = _storage.FileShareName,
            fileName = safeName,
            bytes = bytes.Length,
            processedUtc = DateTime.UtcNow
        }, cancellationToken);
    }
}
