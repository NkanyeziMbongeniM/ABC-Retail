using ABC.Retail.Models;
using ABC.Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC.Retail.Controllers;

public sealed class ProductsController : Controller
{
    private readonly AzureStorageService _storage;
    public ProductsController(AzureStorageService storage) => _storage = storage;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await _storage.GetProductsAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please complete the product fields correctly.";
            return RedirectToAction(nameof(Index));
        }
        model.RowKey = Guid.NewGuid().ToString("N");
        await _storage.AddProductAsync(model, cancellationToken);
        TempData["Success"] = "Product stored in Azure Table Storage.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _storage.DeleteProductAsync(id, cancellationToken);
        TempData["Success"] = "Product deleted from Azure Tables.";
        return RedirectToAction(nameof(Index));
    }
}
