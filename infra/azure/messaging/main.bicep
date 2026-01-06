param location string = resourceGroup().location
param namespaceName string = 'bolttickets-eventhubs-${uniqueString(resourceGroup().id)}'
param skuName string = 'Standard'
param skuTier string = 'Standard'
param skuCapacity int = 1
param kafkaEnabled bool = true
param topicName string = 'ticket-bookings'
param partitionCount int = 3

resource eventHubsNamespace 'Microsoft.EventHub/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  sku: {
    name: skuName
    tier: skuTier
    capacity: skuCapacity
  }
  properties: {
    kafkaEnabled: kafkaEnabled
    zoneRedundant: false
  }
}

resource eventHub 'Microsoft.EventHub/namespaces/eventhubs@2022-10-01-preview' = {
  parent: eventHubsNamespace
  name: topicName
  properties: {
    messageRetentionInDays: 1
    partitionCount: partitionCount
    status: 'Active'
  }
}

resource consumerGroup 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2022-10-01-preview' = {
  parent: eventHub
  name: 'worker'
  properties: {}
}

output namespaceName string = eventHubsNamespace.name
output eventHubName string = eventHub.name
output consumerGroupName string = consumerGroup.name
output primaryConnectionString string = eventHubsNamespace.listKeys('RootManageSharedAccessKey').primaryConnectionString