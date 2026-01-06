param location string = resourceGroup().location
param serverName string = 'bolttickets-postgres-${uniqueString(resourceGroup().id)}'
param administratorLogin string = 'postgresadmin'
param administratorPassword string = 'ChangeMe123!' // Use Key Vault in production
param skuName string = 'B_Gen5_1'
param skuTier string = 'Basic'
param skuCapacity int = 1
param version string = '13'
param storageSizeGB int = 32
param backupRetentionDays int = 7
param geoRedundantBackup string = 'Disabled'

resource postgresServer 'Microsoft.DBforPostgreSQL/servers@2022-12-01' = {
  name: serverName
  location: location
  sku: {
    name: skuName
    tier: skuTier
    capacity: skuCapacity
  }
  properties: {
    version: version
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    storageProfile: {
      storageMB: storageSizeGB * 1024
      backupRetentionDays: backupRetentionDays
      geoRedundantBackup: geoRedundantBackup
    }
    publicNetworkAccess: 'Enabled'
    sslEnforcement: 'Enabled'
  }
}

resource postgresFirewall 'Microsoft.DBforPostgreSQL/servers/firewallRules@2022-12-01' = {
  parent: postgresServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '255.255.255.255'
  }
}

resource postgresDatabase 'Microsoft.DBforPostgreSQL/servers/databases@2022-12-01' = {
  parent: postgresServer
  name: 'bolttickets'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

output serverName string = postgresServer.name
output fullyQualifiedDomainName string = postgresServer.properties.fullyQualifiedDomainName
output databaseName string = postgresDatabase.name
output administratorLogin string = postgresServer.properties.administratorLogin