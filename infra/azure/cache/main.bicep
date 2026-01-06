param location string = resourceGroup().location
param redisName string = 'bolttickets-redis-${uniqueString(resourceGroup().id)}'
param skuName string = 'Basic'
param skuFamily string = 'C'
param skuCapacity int = 0
param redisVersion string = '6'
param enableNonSslPort bool = false

resource redis 'Microsoft.Cache/Redis@2023-04-01' = {
  name: redisName
  location: location
  properties: {
    sku: {
      name: skuName
      family: skuFamily
      capacity: skuCapacity
    }
    redisVersion: redisVersion
    enableNonSslPort: enableNonSslPort
    minimumTlsVersion: '1.2'
  }
}

output redisName string = redis.name
output hostName string = redis.properties.hostName
output sslPort int = redis.properties.sslPort
output primaryKey string = redis.listKeys().primaryKey