using System.Text.Json;
using ABC.Retail.Models;
using ABC.Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC.Retail.Controllers;

public sealed class OrdersController : Controller
{
    private readonly AzureStorageService _storage;
    private readonly AzureFunctionClient _functions;

    public OrdersController(AzureStorageService storage, AzureFunctionClient functions)
    {
        _storage = storage;
        _functions = functions;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _storage.GetOrdersAsync(cancellationToken));

    [HttpPost]
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

        if (_functions.IsConfigured)
        {
            await _functions.StoreOrderAsync(model, cancellationToken);
            await _functions.SendQueueTransactionAsync(JsonSerializer.Serialize(new
            {
                Type = "ProcessOrder",
                model.OrderNumber,
                model.CustomerName,
                model.ProductName,
                model.Quantity,
                model.Total,
                CreatedUtc = model.CreatedUtc
            }), cancellationToken);
            await _functions.SendQueueTransactionAsync(JsonSerializer.Serialize(new
            {
                Type = "UpdateInventory",
                model.OrderNumber,
                model.ProductName,
                QuantityToDeduct = model.Quantity
            }), cancellationToken);

            TempData["Success"] = "Order stored via Azure Table Function and two transaction messages written via Queue Function.";
        }
        else
        {
            await _storage.AddOrderAsync(model, cancellationToken);
            TempData["Success"] = "Order stored directly and queue messages created (Function fallback mode).";
        }

        return RedirectToAction(nameof(Index));
    }
}
