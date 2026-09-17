using ABC.Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC.Retail.Controllers;

public sealed class LogsController : Controller
{
    private readonly AzureStorageService _storage;
    private readonly AzureFunctionClient _functions;

    public LogsController(AzureStorageService storage, AzureFunctionClient functions)
    {
        _storage = storage;
        _functions = functions;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.FunctionMode = _functions.IsConfigured;
        return View(await _storage.GetFilesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Choose a file to send to Azure Files.";
            return RedirectToAction(nameof(Index));
        }
        if (file.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] = "Keep the Project 2 Azure Files demonstration under 5 MB.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        if (_functions.IsConfigured)
        {
            await _functions.WriteAzureFileAsync(stream, file.FileName, file.ContentType, cancellationToken);
            TempData["Success"] = "File sent to Azure Files through WriteAzureFile Azure Function.";
        }
        else
        {
            TempData["Error"] =
                "Azure Functions are not configured. Configure the Function App URL and key first.";

            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Download(string name, CancellationToken cancellationToken)
    {
        var result = await _storage.DownloadFileAsync(name, cancellationToken);
        return File(result.Stream, "application/octet-stream", result.FileName);
    }
}
