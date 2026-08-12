# 2–3 MINUTE DEMONSTRATION SCRIPT

## Opening

“This is ABC Retail, an online retail web application developed using ASP.NET Core MVC and Azure Storage Services. The application addresses the business scenario where the old on-premises system struggled with transaction volume, media storage, messaging reliability and log management.”

## Tables

“Customer, product and order information is stored using Azure Tables. I can add a customer or product from the application and the data is persisted as Azure Table entities.”

## Blobs

“Product images and multimedia are handled by Azure Blob Storage. The Media page allows me to upload and download content. The container is separate from the structured customer and order data.”

## Queues

“When an order is created, the order is stored in Azure Tables and two asynchronous messages are sent to Azure Queue Storage: ProcessOrder and UpdateInventory. This demonstrates decoupling order capture from downstream processing.”

## Files

“Application log files are stored in Azure Files. This keeps operational log data in persistent cloud storage rather than relying on the App Service local filesystem.”

## Scalability/reliability/cost

“The design separates structured data, object media, asynchronous messages and files according to their storage requirements. Azure-managed services provide scalable storage, while the queue allows processing to happen asynchronously.”

## Deployment

“The same application was tested locally and then published to Azure App Service. The deployed application is accessed through its HTTPS App Service URL.”
