using ABC.Retail.Models;
using ABC.Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC.Retail.Controllers;

public sealed class OrdersController : Controller
{
    private readonly AzureStorageService _storage;
    public OrdersController(AzureStorageService storage) => _storage = storage;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await _storage.GetOrdersAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Order model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.Quantity <= 0 || model.UnitPrice < 0)
        {
            TempData["Error"] = "Customer, product, quantity and unit price must be valid.";
            return RedirectToAction(nameof(Index));
        }

        model.RowKey = Guid.NewGuid().ToString("N");
        model.OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        model.Total = model.UnitPrice * model.Quantity;
        model.Status = "Queued";
        model.CreatedUtc = DateTime.UtcNow;

        await _storage.AddOrderAsync(model, cancellationToken);
        TempData["Success"] = "Order stored in Azure Tables. ProcessOrder and UpdateInventory messages were sent to Azure Queue Storage.";
        return RedirectToAction(nameof(Index));
    }
}
