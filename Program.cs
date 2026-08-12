using ABC.Retail.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddSingleton<AzureStorageService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/health", (AzureStorageService storage) => Results.Ok(new
{
    application = "ABC Retail",
    status = storage.IsConfigured ? "Configured" : "NeedsConfiguration",
    storageAccount = storage.StorageAccountLabel,
    utc = DateTime.UtcNow
}));

// Create the required Azure Storage resources and demonstration records when the app starts.
// If the connection string is missing, the web UI remains available and explains what to configure.
await app.Services.GetRequiredService<AzureStorageService>().InitializeAsync();

app.Run();
