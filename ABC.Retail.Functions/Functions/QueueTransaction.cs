using System.Net;
using ABC.Retail.Functions.Models;
using ABC.Retail.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABC.Retail.Functions.Functions;

public sealed class QueueTransaction
{
    private readonly AzureStorageClients _storage;
    private readonly ILogger<QueueTransaction> _logger;

    public QueueTransaction(AzureStorageClients storage, ILogger<QueueTransaction> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [Function("QueueTransaction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "queue/transaction")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var queue = _storage.Queues.GetQueueClient(_storage.QueueName);
        await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        if (string.Equals(req.Method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            var peeked = await queue.PeekMessagesAsync(maxMessages: 16, cancellationToken: cancellationToken);
            var messages = peeked.Value.Select(message => new
            {
                messageId = message.MessageId,
                message = message.Body.ToString(),
                insertedOn = message.InsertedOn,
                expiresOn = message.ExpiresOn
            }).ToList();

            return await FunctionHttp.JsonAsync(req, HttpStatusCode.OK, new
            {
                success = true,
                function = "QueueTransaction",
                operation = "read/peek",
                queue = _storage.QueueName,
                count = messages.Count,
                messages
            }, cancellationToken);
        }

        var input = await FunctionHttp.ReadJsonAsync<QueueWriteRequest>(req, cancellationToken);
        if (input is null || string.IsNullOrWhiteSpace(input.Message))
        {
            return await FunctionHttp.JsonAsync(req, HttpStatusCode.BadRequest,
                new { error = "message is required." }, cancellationToken);
        }

        var send = await queue.SendMessageAsync(input.Message.Trim(), cancellationToken: cancellationToken);
        _logger.LogInformation("Function wrote message {MessageId} to {QueueName}.", send.Value.MessageId, _storage.QueueName);

        return await FunctionHttp.JsonAsync(req, HttpStatusCode.OK, new
        {
            success = true,
            function = "QueueTransaction",
            operation = "write",
            queue = _storage.QueueName,
            messageId = send.Value.MessageId,
            processedUtc = DateTime.UtcNow
        }, cancellationToken);
    }
}
