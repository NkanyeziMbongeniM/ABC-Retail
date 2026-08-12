param(
  [Parameter(Mandatory=$true)][string]$ResourceGroup,
  [Parameter(Mandatory=$true)][string]$AppServiceName
)
$ErrorActionPreference = 'Stop'
dotnet publish .\ABC.Retail.csproj -c Release -o .\publish
Compress-Archive -Path .\publish\* -DestinationPath .\abc-retail-publish.zip -Force
az webapp deploy --resource-group $ResourceGroup --name $AppServiceName --src-path .\abc-retail-publish.zip --type zip
Write-Host "Deployment complete. Open https://$AppServiceName.azurewebsites.net" -ForegroundColor Green
