using ABC.Retail.Models;
using ABC.Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC.Retail.Controllers;

public sealed class CustomersController : Controller
{
    private readonly AzureStorageService _storage;
    public CustomersController(AzureStorageService storage) => _storage = storage;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await _storage.GetCustomersAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please complete all customer fields correctly.";
            return RedirectToAction(nameof(Index));
        }
        model.RowKey = Guid.NewGuid().ToString("N");
        await _storage.AddCustomerAsync(model, cancellationToken);
        TempData["Success"] = "Customer stored in Azure Table Storage.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _storage.DeleteCustomerAsync(id, cancellationToken);
        TempData["Success"] = "Customer deleted from Azure Tables.";
        return RedirectToAction(nameof(Index));
    }
}
