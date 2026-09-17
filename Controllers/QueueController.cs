using ABC.Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC.Retail.Controllers;

public sealed class QueueController : Controller
{
    private readonly AzureStorageService _storage;
    private readonly AzureFunctionClient _functions;

    public QueueController(AzureStorageService storage, AzureFunctionClient functions)
    {
        _storage = storage;
        _functions = functions;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var messages = _functions.IsConfigured
            ? await _functions.PeekQueueAsync(cancellationToken)
            : await _storage.PeekQueueAsync(32, cancellationToken);
        ViewBag.FunctionMode = _functions.IsConfigured;
        return View(messages);
    }

    [HttpPost]
    public async Task<IActionResult> Send(string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Enter a message before sending.";
            return RedirectToAction(nameof(Index));
        }

        if (_functions.IsConfigured)
        {
            await _functions.SendQueueTransactionAsync(message.Trim(), cancellationToken);
            TempData["Success"] = "Message written through QueueTransaction Azure Function.";
        }
        else
        {
            await _storage.SendQueueMessageAsync(message.Trim(), cancellationToken);
            TempData["Success"] = "Message written directly to Azure Queue Storage (Function fallback mode).";
        }
        return RedirectToAction(nameof(Index));
    }
}
