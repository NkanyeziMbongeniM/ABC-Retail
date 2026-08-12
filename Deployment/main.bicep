@description('Unique Azure location for all resources.')
param location string = resourceGroup().location
@description('Globally unique lowercase storage account name, 3-24 characters.')
param storageAccountName string
@description('Globally unique App Service name.')
param appServiceName string
@description('Name of the App Service plan.')
param appServicePlanName string = '${appServiceName}-plan'

resource storage 'Microsoft.Storage/storageAccounts@2025-06-01' = {
  name: storageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

resource customerTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  name: '${storage.name}/default/Customers'
}
resource productTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  name: '${storage.name}/default/Products'
}
resource orderTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  name: '${storage.name}/default/Orders'
}
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  name: '${storage.name}/default'
}
resource blobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: '${storage.name}/default/product-media'
  properties: { publicAccess: 'None' }
}
resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  name: '${storage.name}/default'
}
resource queue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  name: '${storage.name}/default/order-processing'
}
resource fileService 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  name: '${storage.name}/default'
}
resource fileShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  name: '${storage.name}/default/app-logs'
}

resource plan 'Microsoft.Web/serverfarms@2025-03-01' = {
  name: appServicePlanName
  location: location
  sku: { name: 'B1', tier: 'Basic' }
  kind: 'app'
  properties: { reserved: true }
}

resource webapp 'Microsoft.Web/sites@2025-03-01' = {
  name: appServiceName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: false
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT'; value: 'Production' }
      ]
    }
  }
}

output storageAccount string = storage.name
output appServiceUrl string = 'https://${webapp.properties.defaultHostName}'
output appServiceName string = webapp.name
