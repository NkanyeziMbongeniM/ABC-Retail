using ABC.Retail.Models;
using ABC.Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC.Retail.Controllers;

public sealed class CustomersController : Controller
{
    private readonly AzureStorageService _storage;
    private readonly AzureFunctionClient _functions;

    public CustomersController(AzureStorageService storage, AzureFunctionClient functions)
    {
        _storage = storage;
        _functions = functions;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _storage.GetCustomersAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(Customer model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please complete all customer fields correctly.";
            return RedirectToAction(nameof(Index));
        }

        model.RowKey = Guid.NewGuid().ToString("N");
        if (_functions.IsConfigured)
        {
            await _functions.StoreCustomerAsync(model, cancellationToken);
            TempData["Success"] = "Customer stored in Azure Tables through StoreTableInformation Azure Function.";
        }
        else
        {
            await _storage.AddCustomerAsync(model, cancellationToken);
            TempData["Success"] = "Customer stored directly in Azure Tables (Function fallback mode).";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _storage.DeleteCustomerAsync(id, cancellationToken);
        TempData["Success"] = "Customer deleted from Azure Tables.";
        return RedirectToAction(nameof(Index));
    }
}
