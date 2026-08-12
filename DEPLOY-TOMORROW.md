# TOMORROW DEPLOYMENT PLAN — ABC RETAIL

## 1. Before opening Visual Studio

- [ ] Download/extract this project.
- [ ] Confirm `.NET 9 SDK` or Visual Studio with .NET 9 support is installed.
- [ ] Sign into Azure Portal.
- [ ] Confirm you have an Azure subscription.
- [ ] Create or identify one Storage Account.

## 2. Storage account

Use a simple student-friendly configuration:

- Performance: Standard
- Redundancy: LRS
- Secure transfer: Enabled
- Blob public access: Disabled

Copy the Storage connection string.

## 3. Local test

Open `ABC.Retail.csproj`.

Configure:

`ConnectionStrings:AzureStorage`

Run the app.

Open the Dashboard.

The first successful connection automatically creates:

- Customers table
- Products table
- Orders table
- product-media blob container
- order-processing queue
- app-logs file share

It also creates at least five demonstration records in each evidence category.

## 4. Test every feature

### Customers
Add one customer. Refresh. Confirm it appears.

### Products
Add one product. Refresh. Confirm it appears.

### Orders
Create an order. Confirm the order appears in the Orders table and that queue messages appear.

### Blob Media
Upload a test image/PDF/text file. Confirm it appears and download it again.

### Queue
Send a test message. Confirm it appears when the page is refreshed.

### Logs
Confirm log files are visible and downloadable.

## 5. Azure Portal evidence

Take screenshots of the actual resources. The lecturer asked for at least five records, so make sure the screenshot clearly shows at least five rows/items.

Capture:

1. Customers table
2. Products table
3. Orders table
4. Blob container
5. Queue
6. File share

## 6. Publish

Visual Studio:

Project → Publish → Azure → Azure App Service → Create/Select App Service → Publish.

After deployment, configure the Storage connection string in App Service Configuration/Connection strings.

Restart the App Service.

Open the generated HTTPS URL.

## 7. Final deployed test

- [ ] Dashboard loads.
- [ ] Azure Storage Connected is displayed.
- [ ] Customers page loads.
- [ ] Products page loads.
- [ ] Orders page loads.
- [ ] Blob upload works.
- [ ] Blob download works.
- [ ] Queue messages display.
- [ ] Azure Files logs display.
- [ ] `/health` returns JSON.

## 8. GitHub

Before pushing:

- [ ] Search the repository for `DefaultEndpointsProtocol=`.
- [ ] Search for `AccountKey=`.
- [ ] Search for real passwords/secrets.
- [ ] Confirm no real Azure connection string is committed.
- [ ] Push source code.
- [ ] Copy the repository URL for the submission document.

## 9. Submission document

Include:

- Screenshots of each Azure Storage service with at least five records where required.
- Screenshots of the deployed application.
- Actual App Service URL.
- Actual GitHub repository URL.
- Short explanation of how Tables, Blobs, Queues and Files were used.
- Testing evidence from both local and deployed environments.
