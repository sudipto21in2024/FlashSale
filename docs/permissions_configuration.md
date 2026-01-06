# Permissions and Configuration Tutorial

This tutorial provides comprehensive guidance for setting up permissions and configurations required to deploy BoltTickets to Azure and AWS environments.

## Azure Environment Setup

### 1. Azure CLI Installation and Configuration

#### Install Azure CLI
```bash
# Windows (PowerShell)
winget install -e --id Microsoft.AzureCLI

# macOS
brew update && brew install azure-cli

# Linux
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

#### Authenticate Azure CLI
```bash
# Interactive login
az login

# Service Principal login (for CI/CD)
az login --service-principal -u <app-id> -p <password> -t <tenant-id>
```

#### Set Default Subscription
```bash
# List subscriptions
az account list --output table

# Set default subscription
az account set --subscription <subscription-id>
```

### 2. Azure Permissions and Roles

#### Required Azure RBAC Roles

**Subscription Level:**
- `Contributor` - For resource creation and management
- `User Access Administrator` - For managing role assignments

**Resource Group Level:**
- `Contributor` - Full access to resources
- `AcrPush` - For pushing images to ACR
- `AcrPull` - For pulling images from ACR

#### Create Custom Role for BoltTickets (Optional)
```json
{
  "Name": "BoltTickets Contributor",
  "IsCustom": true,
  "Description": "Custom role for BoltTickets deployment",
  "Actions": [
    "Microsoft.ContainerRegistry/registries/*",
    "Microsoft.ContainerService/managedClusters/*",
    "Microsoft.DBforPostgreSQL/servers/*",
    "Microsoft.Cache/Redis/*",
    "Microsoft.EventHub/namespaces/*",
    "Microsoft.Network/virtualNetworks/*",
    "Microsoft.Network/networkSecurityGroups/*",
    "Microsoft.KeyVault/vaults/*"
  ],
  "NotActions": [],
  "AssignableScopes": [
    "/subscriptions/<subscription-id>"
  ]
}
```

#### Assign Roles
```bash
# Assign role to user
az role assignment create \
  --assignee <user-principal-name> \
  --role "Contributor" \
  --scope /subscriptions/<subscription-id>

# Assign role to service principal
az role assignment create \
  --assignee <service-principal-id> \
  --role "Contributor" \
  --scope /subscriptions/<subscription-id>/resourceGroups/<resource-group>
```

### 3. Azure DevOps Service Connections

#### Azure Resource Manager Connection
1. Go to Azure DevOps → Project Settings → Service connections
2. Click "New service connection" → "Azure Resource Manager"
3. Select "Service principal (automatic)"
4. Select subscription and resource group
5. Name: `azure-subscription`
6. Grant access permission to all pipelines

#### Azure Container Registry Connection
1. Go to Service connections → "New service connection" → "Docker Registry"
2. Select "Azure Container Registry"
3. Select subscription and ACR
4. Name: `acr-connection`

#### Kubernetes Service Connection
1. Go to Service connections → "New service connection" → "Kubernetes"
2. Select "Azure Kubernetes Service"
3. Select subscription, resource group, and AKS cluster
4. Name: `aks-connection`

### 4. Azure Key Vault Setup

#### Create Key Vault
```bash
az keyvault create \
  --name bolttickets-kv \
  --resource-group bolttickets-rg \
  --location eastus \
  --enabled-for-deployment true \
  --enabled-for-template-deployment true
```

#### Store Secrets
```bash
# Database password
az keyvault secret set \
  --vault-name bolttickets-kv \
  --name db-password \
  --value "YourSecurePassword123!"

# Redis password (if applicable)
az keyvault secret set \
  --vault-name bolttickets-kv \
  --name redis-password \
  --value "YourRedisPassword123!"
```

#### Grant Access to Service Principal
```bash
az keyvault set-policy \
  --name bolttickets-kv \
  --spn <service-principal-id> \
  --secret-permissions get list
```

## AWS Environment Setup

### 1. AWS CLI Installation and Configuration

#### Install AWS CLI v2
```bash
# Linux/macOS
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install

# Windows
msiexec.exe /i https://awscli.amazonaws.com/AWSCLIV2.msi
```

#### Configure AWS CLI
```bash
# Interactive configuration
aws configure

# Enter:
# AWS Access Key ID: [Your access key]
# AWS Secret Access Key: [Your secret key]
# Default region name: us-east-1
# Default output format: json

# Named profile configuration
aws configure --profile bolttickets
```

#### Verify Configuration
```bash
# Check current configuration
aws configure list

# Test access
aws sts get-caller-identity
```

### 2. AWS IAM Permissions

#### Required IAM Policies

**AdministratorAccess (Development Only):**
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "*",
      "Resource": "*"
    }
  ]
}
```

**Production IAM Policy:**
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "eks:*",
        "ec2:*",
        "iam:*",
        "rds:*",
        "elasticache:*",
        "msk:*",
        "ecr:*",
        "kms:*",
        "secretsmanager:*",
        "ssm:*"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "sts:AssumeRole"
      ],
      "Resource": "*"
    }
  ]
}
```

#### Create IAM User for Deployment
```bash
# Create IAM user
aws iam create-user --user-name bolttickets-deploy

# Create access keys
aws iam create-access-key --user-name bolttickets-deploy

# Attach policies
aws iam attach-user-policy \
  --user-name bolttickets-deploy \
  --policy-arn arn:aws:iam::aws:policy/AdministratorAccess
```

#### Create IAM Role for EKS
```bash
# Create EKS cluster role
aws iam create-role \
  --role-name BoltTickets-EKS-Cluster-Role \
  --assume-role-policy-document file://eks-cluster-role.json

# Attach required policies
aws iam attach-role-policy \
  --role-name BoltTickets-EKS-Cluster-Role \
  --policy-arn arn:aws:iam::aws:policy/AmazonEKSClusterPolicy

# Create EKS node role
aws iam create-role \
  --role-name BoltTickets-EKS-Node-Role \
  --assume-role-policy-document file://eks-node-role.json

aws iam attach-role-policy \
  --role-name BoltTickets-EKS-Node-Role \
  --policy-arn arn:aws:iam::aws:policy/AmazonEKSWorkerNodePolicy

aws iam attach-role-policy \
  --role-name BoltTickets-EKS-Node-Role \
  --policy-arn arn:aws:iam::aws:policy/AmazonEKS_CNI_Policy

aws iam attach-role-policy \
  --role-name BoltTickets-EKS-Node-Role \
  --policy-arn arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryReadOnly
```

### 3. AWS Secrets Manager Setup

#### Create Secrets
```bash
# Database password
aws secretsmanager create-secret \
  --name bolttickets/db-password \
  --secret-string "YourSecurePassword123!"

# Redis password (if applicable)
aws secretsmanager create-secret \
  --name bolttickets/redis-password \
  --secret-string "YourRedisPassword123!"

# Kafka connection string
aws secretsmanager create-secret \
  --name bolttickets/kafka-connection \
  --secret-string "your-kafka-bootstrap-servers"
```

#### Grant Access to IAM User/Role
```bash
# Create policy for secrets access
aws iam create-policy \
  --policy-name BoltTickets-Secrets-Policy \
  --policy-document file://secrets-policy.json

# Attach to user/role
aws iam attach-user-policy \
  --user-name bolttickets-deploy \
  --policy-arn arn:aws:iam::<account-id>:policy/BoltTickets-Secrets-Policy
```

## Environment-Specific Configurations

### Application Configuration

#### Azure Configuration
```yaml
# k8s/configmaps.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: api-config
  namespace: bolttickets
data:
  ASPNETCORE_ENVIRONMENT: Production
  ConnectionStrings__DefaultConnection: "Host=bolttickets-postgres.postgres.database.azure.com;Database=bolttickets;Username=postgres@bolttickets-postgres;Password=@Microsoft.KeyVault(SecretUri=https://bolttickets-kv.vault.azure.net/secrets/db-password/)"
  Redis__ConnectionString: "bolttickets-redis.redis.cache.windows.net:6380,password=@Microsoft.KeyVault(SecretUri=https://bolttickets-kv.vault.azure.net/secrets/redis-password/),ssl=True"
  Kafka__BootstrapServers: "bolttickets-eventhubs.servicebus.windows.net:9093"
```

#### AWS Configuration
```yaml
# k8s/configmaps.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: api-config
  namespace: bolttickets
data:
  ASPNETCORE_ENVIRONMENT: Production
  ConnectionStrings__DefaultConnection: "Host=bolttickets-postgres.c123456789abc.us-east-1.rds.amazonaws.com;Database=bolttickets;Username=postgres;Password=@aws:secretsmanager:bolttickets/db-password"
  Redis__ConnectionString: "bolttickets-redis.abcdef.ng.0001.use1.cache.amazonaws.com:6379,password=@aws:secretsmanager:bolttickets/redis-password"
  Kafka__BootstrapServers: "@aws:secretsmanager:bolttickets/kafka-connection"
```

### Network Configuration

#### Azure Networking
```bash
# Create VNet
az network vnet create \
  --resource-group bolttickets-rg \
  --name bolttickets-vnet \
  --address-prefix 10.0.0.0/16 \
  --subnet-name default \
  --subnet-prefix 10.0.0.0/24

# Create NSG
az network nsg create \
  --resource-group bolttickets-rg \
  --name bolttickets-nsg

# Add security rules
az network nsg rule create \
  --resource-group bolttickets-rg \
  --nsg-name bolttickets-nsg \
  --name Allow-HTTP \
  --priority 100 \
  --destination-port-ranges 80 443 \
  --access Allow \
  --protocol Tcp
```

#### AWS Networking
```bash
# Create VPC
aws ec2 create-vpc \
  --cidr-block 10.0.0.0/16 \
  --tag-specification ResourceType=vpc,Tags='[{Key:Name,Value=bolttickets-vpc}]'

# Create subnets
aws ec2 create-subnet \
  --vpc-id vpc-12345678 \
  --cidr-block 10.0.1.0/24 \
  --availability-zone us-east-1a \
  --tag-specification ResourceType=subnet,Tags='[{Key:Name,Value=bolttickets-private-1a}]'

# Create security groups
aws ec2 create-security-group \
  --group-name bolttickets-web-sg \
  --description "Security group for web servers" \
  --vpc-id vpc-12345678

aws ec2 authorize-security-group-ingress \
  --group-id sg-12345678 \
  --protocol tcp \
  --port 80 \
  --cidr 0.0.0.0/0
```

## CI/CD Pipeline Configuration

### Azure DevOps Variables
```
# Azure
acrName: boltticketsacr
resourceGroup: bolttickets-rg
location: eastus
aksName: bolttickets-aks
namespace: bolttickets

# Application
apiImage: bolttickets/api
workerImage: bolttickets/worker
frontendImage: bolttickets/frontend
```

### GitHub Actions (Alternative)
```yaml
# .github/workflows/deploy.yml
name: Deploy to AWS
on:
  push:
    branches: [ main ]

env:
  AWS_REGION: us-east-1
  ECR_REPOSITORY: bolttickets
  EKS_CLUSTER: bolttickets-eks

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3

    - name: Configure AWS credentials
      uses: aws-actions/configure-aws-credentials@v2
      with:
        aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
        aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
        aws-region: us-east-1

    - name: Login to Amazon ECR
      id: login-ecr
      uses: aws-actions/amazon-ecr-login@v1

    - name: Build and push API image
      run: |
        docker build -t $ECR_REGISTRY/$ECR_REPOSITORY/api:$GITHUB_SHA -f src/BoltTickets.API/Dockerfile .
        docker push $ECR_REGISTRY/$ECR_REPOSITORY/api:$GITHUB_SHA

    - name: Deploy to EKS
      run: |
        aws eks update-kubeconfig --name $EKS_CLUSTER
        kubectl apply -f k8s/
        kubectl set image deployment/api api=$ECR_REGISTRY/$ECR_REPOSITORY/api:$GITHUB_SHA
```

## Monitoring and Alerting Setup

### Azure Monitor
```bash
# Enable AKS monitoring
az monitor diagnostic-settings create \
  --name aks-monitoring \
  --resource /subscriptions/<sub-id>/resourceGroups/bolttickets-rg/providers/Microsoft.ContainerService/managedClusters/bolttickets-aks \
  --logs '[{"category": "kube-apiserver", "enabled": true}]' \
  --metrics '[{"category": "AllMetrics", "enabled": true}]' \
  --workspace /subscriptions/<sub-id>/resourceGroups/DefaultResourceGroup/providers/Microsoft.OperationalInsights/workspaces/DefaultWorkspace
```

### CloudWatch (AWS)
```bash
# Enable EKS monitoring
aws eks update-cluster-config \
  --name bolttickets-eks \
  --logging '{"clusterLogging":[{"types":["api","audit","authenticator","controllerManager","scheduler"],"enabled":true}]}'

# Create CloudWatch alarms
aws cloudwatch put-metric-alarm \
  --alarm-name "HighCPU" \
  --alarm-description "CPU utilization is high" \
  --metric-name CPUUtilization \
  --namespace AWS/EC2 \
  --statistic Average \
  --period 300 \
  --threshold 80 \
  --comparison-operator GreaterThanThreshold
```

## Backup and Disaster Recovery

### Azure Backup
```bash
# Enable AKS backup
az k8s-extension create \
  --name aks-backup \
  --extension-type Microsoft.DataProtection.Kubernetes \
  --scope cluster \
  --cluster-name bolttickets-aks \
  --resource-group bolttickets-rg \
  --cluster-type managedClusters \
  --configuration-settings blobContainer=backup-container storageAccount=backupstorage
```

### AWS Backup
```bash
# Create backup vault
aws backup create-backup-vault \
  --backup-vault-name bolttickets-backup-vault

# Create backup plan
aws backup create-backup-plan \
  --backup-plan file://backup-plan.json
```

## Troubleshooting Common Issues

### Permission Denied Errors
- **Azure**: Check RBAC roles and service principal permissions
- **AWS**: Verify IAM policies and trust relationships

### Authentication Failures
- **Azure**: Refresh service principal credentials
- **AWS**: Rotate access keys and update configurations

### Network Connectivity
- **Azure**: Verify NSG rules and VNet peering
- **AWS**: Check security groups and NACLs

### Resource Quotas
- **Azure**: Check subscription limits and request increases
- **AWS**: Monitor service limits and request quota increases

This comprehensive setup ensures secure, properly configured environments for BoltTickets deployment on both Azure and AWS platforms.