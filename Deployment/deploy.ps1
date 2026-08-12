param(
  [Parameter(Mandatory=$true)][string]$ResourceGroup,
  [Parameter(Mandatory=$true)][string]$Location,
  [Parameter(Mandatory=$true)][string]$StorageAccountName,
  [Parameter(Mandatory=$true)][string]$AppServiceName
)

$ErrorActionPreference = 'Stop'
Write-Host "Creating/updating Azure resources..." -ForegroundColor Cyan
az group create --name $ResourceGroup --location $Location | Out-Null
az deployment group create `
  --resource-group $ResourceGroup `
  --template-file .\Deployment\main.bicep `
  --parameters storageAccountName=$StorageAccountName appServiceName=$AppServiceName location=$Location

Write-Host "Resources created. Next: publish the .NET project to the App Service and configure the connection string." -ForegroundColor Green
