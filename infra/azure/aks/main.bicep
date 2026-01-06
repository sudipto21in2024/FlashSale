param location string = resourceGroup().location
param clusterName string = 'bolttickets-aks'
param dnsPrefix string = 'bolttickets'
param agentCount int = 3
param agentVMSize string = 'Standard_DS2_v2'
param kubernetesVersion string = '1.28.5'
param enableRBAC bool = true
param enableAzureMonitor bool = true

resource aks 'Microsoft.ContainerService/managedClusters@2023-09-02-preview' = {
  name: clusterName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    kubernetesVersion: kubernetesVersion
    dnsPrefix: dnsPrefix
    agentPoolProfiles: [
      {
        name: 'agentpool'
        count: agentCount
        vmSize: agentVMSize
        osType: 'Linux'
        mode: 'System'
      }
    ]
    enableRBAC: enableRBAC
    addonProfiles: {
      omsAgent: {
        enabled: enableAzureMonitor
        config: enableAzureMonitor ? {
          logAnalyticsWorkspaceResourceID: logAnalytics.id
        } : {}
      }
    }
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = if (enableAzureMonitor) {
  name: '${clusterName}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

output clusterName string = aks.name
output clusterResourceGroup string = aks.properties.nodeResourceGroup
output kubeConfig string = aks.listClusterUserCredential().kubeconfigs[0].value