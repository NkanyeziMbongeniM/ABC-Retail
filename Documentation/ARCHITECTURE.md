# Architecture Notes

```text
Browser
  |
  v
ASP.NET Core MVC
  |
  +--> Azure.Data.Tables
  |       +--> Customers
  |       +--> Products
  |       +--> Orders
  |
  +--> Azure.Storage.Blobs
  |       +--> product-media
  |
  +--> Azure.Storage.Queues
  |       +--> order-processing
  |
  +--> Azure.Storage.Files.Shares
          +--> app-logs
```

## Why the separation matters

- Tables provide structured entity storage without requiring a traditional relational database.
- Blobs are optimized for object/media storage and do not inflate the table records with binary content.
- Queues provide a durable asynchronous work backlog.
- Azure Files provides persistent file-share semantics for log files.

The application is intentionally simple enough to demonstrate the concepts clearly while still providing working create/read/delete and upload/download workflows.
