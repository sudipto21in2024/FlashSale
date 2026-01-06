param location string = resourceGroup().location
param registryName string = 'boltticketsacr${uniqueString(resourceGroup().id)}'
param sku string = 'Basic'

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: true
    publicNetworkAccess: 'Enabled'
  }
}

output registryName string = acr.name
output loginServer string = acr.properties.loginServer
output adminUsername string = acr.listCredentials().username
output adminPassword string = acr.listCredentials().passwords[0].value