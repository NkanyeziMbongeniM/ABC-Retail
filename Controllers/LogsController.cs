using ABC.Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC.Retail.Controllers;

public sealed class LogsController : Controller
{
    private readonly AzureStorageService _storage;
    public LogsController(AzureStorageService storage) => _storage = storage;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await _storage.GetFilesAsync(cancellationToken));

    public async Task<IActionResult> Download(string name, CancellationToken cancellationToken)
    {
        var result = await _storage.DownloadFileAsync(name, cancellationToken);
        return File(result.Stream, "text/plain", result.FileName);
    }
}
