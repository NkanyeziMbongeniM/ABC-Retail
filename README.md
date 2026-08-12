# ABC Retail — CLDV7112w Project 1

A complete ASP.NET Core MVC demonstration application implementing the Azure Storage requirements in the Project 1 brief.

## Assignment requirements mapped to the application

| Brief requirement | Application implementation |
|---|---|
| Store customer and product-related information using Azure Tables | `Customers`, `Products`, and `Orders` tables are created and used by the application. |
| Host images and multimedia using Azure Blob Storage | `product-media` container supports upload, listing, download and deletion. |
| Store order processing and inventory details in Azure Queues | `order-processing` queue receives `ProcessOrder` and `UpdateInventory` messages whenever an order is created. |
| Store log files using Azure Files | `app-logs` share stores daily application logs and five seeded evidence files. |
| Consider scalability, reliability and cost effectiveness | Azure-managed storage services are used instead of a local database/file system. Queue work is asynchronous, Blob media is separate from structured data, and the demo uses Standard LRS storage. |
| Upload/download/display controls | Customer, Product, Order, Media, Queue and Logs pages provide working controls. |
| Test locally and on App Service | The project can run locally with an Azure Storage connection string and can be published to Azure App Service. |

## Architecture

```text
                         ABC Retail ASP.NET Core MVC
                                  |
          +-----------------------+-----------------------+
          |                       |                       |
     Azure Tables           Blob Storage            Queue Storage
          |                       |                       |
 Customers / Products       Product media          Order processing
 Orders / Inventory         uploads/downloads      + inventory messages
          |
          +----------------------+
                                 |
                           Azure Files
                           application logs
```

Azure Storage is accessed through the official Azure SDK packages for .NET. Microsoft documents Blob, Queue, Table and File services as separate Azure Storage data services and supports their programmatic access from .NET applications.

## Prerequisites

- Visual Studio 2022 with ASP.NET/.NET web development workload, or the current .NET 9 SDK.
- An Azure subscription.
- An Azure Storage account.
- Azure CLI if you want to use the included deployment scripts.
- A GitHub account if the lecturer requires the source-code repository link.

## Run locally

1. Open `ABC.Retail.csproj` in Visual Studio.
2. Create an Azure Storage account.
3. Copy its connection string from Azure Portal → Storage account → Access keys → Connection string.
4. For local testing, put the value in user secrets or `appsettings.Local.json`. Do not commit a real connection string.
5. The application reads `ConnectionStrings:AzureStorage`.
6. Run the project.
7. On first successful connection, the application creates all required Azure resources and seeds five records for each evidence category.

### Local connection example

```json
{
  "ConnectionStrings": {
    "AzureStorage": "YOUR_STORAGE_CONNECTION_STRING"
  }
}
```

The example above is intentionally a placeholder. Never commit your real key.

## Automatic setup

On startup the application creates:

- Azure Table: `Customers`
- Azure Table: `Products`
- Azure Table: `Orders`
- Blob container: `product-media`
- Queue: `order-processing`
- File share: `app-logs`

The seeder is idempotent. It checks each demo record independently, so deleting one category and restarting the application restores the missing demonstration records without duplicating the others.

## Evidence data

The first successful startup creates at least:

- 5 customer entities
- 5 product entities
- 5 order entities
- 5 queue messages
- 5 demo SVG blobs
- 5 demo Azure Files records

New orders create two additional queue messages: one `ProcessOrder` message and one `UpdateInventory` message.

## Pages

### Dashboard
Shows storage connection status and counts for every Azure service.

### Customers
Create, view and delete customer entities in Azure Tables.

### Products
Create, view and delete product/inventory entities in Azure Tables.

### Orders
Create orders. The application stores the order in Azure Tables and sends processing/inventory messages to Azure Queue Storage.

### Blob Media
Upload, list, download and delete files in the Blob container.

### Queue
Send a test message and peek at existing messages without deleting them.

### Azure Files
List and download application log files stored in the Azure Files share.

### Health endpoint
`/health` returns a small JSON status response that is useful for verifying that the deployed site is running.

## Azure App Service deployment

### Option A — Visual Studio

1. Right-click the project.
2. Select **Publish**.
3. Select **Azure** → **Azure App Service**.
4. Select or create the App Service.
5. Publish the application.
6. In Azure Portal open the App Service → **Environment variables / Configuration**.
7. Add the connection string named `AzureStorage` under connection strings, or set the equivalent application configuration expected by the project.
8. Restart the App Service.
9. Open the generated `https://<app-name>.azurewebsites.net` address.

### Option B — Azure CLI / included scripts

The `Deployment` folder contains:

- `main.bicep` — provisions Storage Account, Tables, Blob container, Queue, File share and App Service plan/web app.
- `deploy.ps1` — creates/updates Azure infrastructure.
- `configure-storage.ps1` — puts the Storage connection string into App Service configuration.
- `publish.ps1` — publishes and deploys the application package.

PowerShell example:

```powershell
az login
.\Deployment\deploy.ps1 -ResourceGroup "rg-abc-retail" -Location "South Africa North" -StorageAccountName "abc<unique>storage" -AppServiceName "abc-retail-<unique>"
.\Deployment\configure-storage.ps1 -ResourceGroup "rg-abc-retail" -StorageAccountName "abc<unique>storage" -AppServiceName "abc-retail-<unique>"
.\Deployment\publish.ps1 -ResourceGroup "rg-abc-retail" -AppServiceName "abc-retail-<unique>"
```

Storage account names must be globally unique and lowercase.

## Submission screenshots

Capture evidence from your own Azure Portal and deployed application. Do not submit invented screenshots.

Recommended evidence set:

1. Azure Storage account overview.
2. `Customers` table with 5+ entities.
3. `Products` table with 5+ entities.
4. `Orders` table with 5+ entities.
5. `product-media` Blob container with 5+ blobs.
6. `order-processing` Queue with 5+ messages.
7. `app-logs` File share with 5+ files.
8. Application dashboard showing all four storage services connected.
9. Customers page.
10. Products page.
11. Orders page.
12. Blob Media page.
13. Queue page.
14. Azure Files/Logs page.
15. Deployed App Service overview showing the web app URL.
16. The deployed website opened through the URL.

## Important security note

Do not commit a Storage Account connection string to GitHub or place it in a screenshot. Use App Service configuration/connection strings for deployment. Microsoft recommends Microsoft Entra ID/managed identities for production Azure Storage authorization where practical; a connection string is used here because it is straightforward for a student demonstration and matches a typical introductory Azure Storage assignment.
