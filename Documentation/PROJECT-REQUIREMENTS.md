# Project Requirements Traceability

The visible project brief requires a web application that makes use of Azure Storage Services.

## Requirement 1 — Azure Tables

Customer and product information is stored in Azure Tables. The application also stores orders in a dedicated Orders table because the brief explicitly discusses order processing and inventory management.

## Requirement 2 — Azure Blob Storage

The Media page uploads product/media files into the `product-media` container. The application lists blobs and provides download functionality.

## Requirement 3 — Azure Queue Storage

The Orders page creates two queue messages for each new order. This represents order processing and inventory management as asynchronous work.

## Requirement 4 — Azure Files

The application writes operational events to the `app-logs` share. Log files can be listed and downloaded.

## Requirement 5 — Scalability, reliability and cost effectiveness

The architecture uses the appropriate storage type for each data category. Azure Tables are used for structured entities, Blobs for binary/media objects, Queues for asynchronous messages, and Files for persistent logs. Standard LRS is suitable for a student demonstration where the assignment prioritizes cost effectiveness.

## Requirement 6 — Local and App Service testing

The same ASP.NET Core project can be run locally with a Storage connection string and published to Azure App Service with the connection configured through App Service settings.
