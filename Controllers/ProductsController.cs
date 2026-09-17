using ABC.Retail.Models;
using ABC.Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC.Retail.Controllers;

public sealed class ProductsController : Controller
{
    private readonly AzureStorageService _storage;
    private readonly AzureFunctionClient _functions;

    public ProductsController(AzureStorageService storage, AzureFunctionClient functions)
    {
        _storage = storage;
        _functions = functions;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _storage.GetProductsAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(Product model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please complete the product fields correctly.";
            return RedirectToAction(nameof(Index));
        }

        model.RowKey = Guid.NewGuid().ToString("N");
        if (_functions.IsConfigured)
        {
            await _functions.StoreProductAsync(model, cancellationToken);
            TempData["Success"] = "Product stored in Azure Tables through StoreTableInformation Azure Function.";
        }
        else
        {
            await _storage.AddProductAsync(model, cancellationToken);
            TempData["Success"] = "Product stored directly in Azure Tables (Function fallback mode).";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _storage.DeleteProductAsync(id, cancellationToken);
        TempData["Success"] = "Product deleted from Azure Tables.";
        return RedirectToAction(nameof(Index));
    }
}
