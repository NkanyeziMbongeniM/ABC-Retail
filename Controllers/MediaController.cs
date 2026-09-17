using ABC.Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC.Retail.Controllers;

public sealed class MediaController : Controller
{
    private readonly AzureStorageService _storage;
    private readonly AzureFunctionClient _functions;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".mp4", ".txt", ".pdf", ".json" };

    public MediaController(AzureStorageService storage, AzureFunctionClient functions)
    {
        _storage = storage;
        _functions = functions;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _storage.GetBlobsAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Choose a file first.";
            return RedirectToAction(nameof(Index));
        }
        if (file.Length > 10 * 1024 * 1024)
        {
            TempData["Error"] = "Keep Project 2 demonstration uploads under 10 MB.";
            return RedirectToAction(nameof(Index));
        }
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            TempData["Error"] = "Allowed examples: JPG, PNG, GIF, WEBP, SVG, MP4, TXT, JSON and PDF.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        if (_functions.IsConfigured)
        {
            await _functions.WriteBlobAsync(stream, file.FileName, file.ContentType, cancellationToken);
            TempData["Success"] = "File written to Azure Blob Storage through WriteBlobStorage Azure Function.";
        }
        else
        {
            await _storage.UploadBlobAsync(stream, file.FileName, file.ContentType, cancellationToken);
            TempData["Success"] = "File written directly to Azure Blob Storage (Function fallback mode).";
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Download(string name, CancellationToken cancellationToken)
    {
        var result = await _storage.DownloadBlobAsync(name, cancellationToken);
        return File(result.Stream, result.ContentType, result.FileName);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string name, CancellationToken cancellationToken)
    {
        await _storage.DeleteBlobAsync(name, cancellationToken);
        TempData["Success"] = "Blob deleted from Azure Blob Storage.";
        return RedirectToAction(nameof(Index));
    }
}
