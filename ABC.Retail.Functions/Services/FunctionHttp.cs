using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace ABC.Retail.Functions.Services;

public static class FunctionHttp
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T?> ReadJsonAsync<T>(HttpRequestData request, CancellationToken cancellationToken = default)
    {
        return await JsonSerializer.DeserializeAsync<T>(request.Body, JsonOptions, cancellationToken);
    }

    public static async Task<HttpResponseData> JsonAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(payload, JsonOptions), cancellationToken);
        return response;
    }
}
