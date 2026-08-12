param(
  [Parameter(Mandatory=$true)][string]$ResourceGroup,
  [Parameter(Mandatory=$true)][string]$StorageAccountName,
  [Parameter(Mandatory=$true)][string]$AppServiceName
)
$ErrorActionPreference = 'Stop'
$conn = az storage account show-connection-string --resource-group $ResourceGroup --name $StorageAccountName --query connectionString -o tsv
az webapp config connection-string set --resource-group $ResourceGroup --name $AppServiceName --connection-string-type Custom --settings AzureStorage="$conn"
Write-Host "AzureStorage connection string configured on the App Service." -ForegroundColor Green
