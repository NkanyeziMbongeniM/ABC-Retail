# Azure Portal / CLI Reference

## Login

```bash
az login
az account show
```

## Create a resource group

```bash
az group create --name rg-abc-retail --location southafricanorth
```

## Create a Standard LRS storage account

```bash
az storage account create --name abcYOURUNIQUESTORAGE --resource-group rg-abc-retail --location southafricanorth --sku Standard_LRS --kind StorageV2 --https-only true
```

## Get the connection string

```bash
az storage account show-connection-string --name abcYOURUNIQUESTORAGE --resource-group rg-abc-retail --query connectionString -o tsv
```

## List storage accounts

```bash
az storage account list --resource-group rg-abc-retail -o table
```

## Publish with Visual Studio

Preferred for a student submission: open `ABC.Retail.csproj`, choose Publish → Azure → Azure App Service.

## Verify App Service

```bash
az webapp show --resource-group rg-abc-retail --name abc-retail-YOURUNIQUE --query defaultHostName -o tsv
```
