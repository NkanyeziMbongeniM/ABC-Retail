using ABC.Retail.Models;
using ABC.Retail.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ABC.Retail.Controllers;

public sealed class HomeController : Controller
{
    private readonly AzureStorageService _storage;
    public HomeController(AzureStorageService storage) => _storage = storage;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await _storage.GetDashboardAsync(cancellationToken));

    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }
}
