using ABC.Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC.Retail.Controllers;

public sealed class QueueController : Controller
{
    private readonly AzureStorageService _storage;
    public QueueController(AzureStorageService storage) => _storage = storage;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await _storage.PeekQueueAsync(32, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Enter a message before sending.";
            return RedirectToAction(nameof(Index));
        }
        await _storage.SendQueueMessageAsync(message.Trim(), cancellationToken);
        TempData["Success"] = "Message added to Azure Queue Storage.";
        return RedirectToAction(nameof(Index));
    }
}
