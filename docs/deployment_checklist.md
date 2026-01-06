# BoltTickets Deployment Checklist

## Azure Deployment Checklist

### Prerequisites & Permissions

#### Azure Account Setup
- [ ] Azure subscription with Owner/Contributor role
- [ ] Resource group creation permissions
- [ ] AKS cluster creation permissions
- [ ] Azure CLI installed and authenticated (`az login`)
- [ ] Bicep CLI installed (`az bicep install`)

**Common Errors:**
- `AuthorizationFailed`: Check role assignments in Azure portal
- `SubscriptionNotFound`: Verify subscription ID and access
- `RegionNotAvailable`: Choose supported region for services

#### Azure DevOps Setup
- [ ] Azure DevOps organization access
- [ ] Project creation permissions
- [ ] Pipeline creation and execution permissions
- [ ] Service connection creation permissions

**Common Errors:**
- `TF401019: The Git repository with name or identifier does not exist`: Check repository permissions
- `Access denied`: Verify Azure DevOps security settings

### Infrastructure Deployment

#### 1. Resource Group
- [ ] Create resource group: `az group create -n bolttickets-rg -l eastus`
- [ ] Verify resource group exists in portal

**Common Errors:**
- `ResourceGroupNotFound`: Check resource group name and location
- `InvalidResourceGroupLocation`: Verify supported locations

#### 2. Container Registry (ACR)
- [ ] Deploy ACR: `az deployment group create --resource-group bolttickets-rg --template-file infra/azure/registry/main.bicep`
- [ ] Verify ACR creation in portal
- [ ] Note ACR login server URL

**Common Errors:**
- `RegistryAlreadyExists`: Choose unique registry name
- `InvalidRegistryName`: Follow naming conventions (3-50 chars, lowercase)

#### 3. AKS Cluster
- [ ] Deploy AKS: `az deployment group create --resource-group bolttickets-rg --template-file infra/azure/aks/main.bicep`
- [ ] Wait for cluster provisioning (10-15 minutes)
- [ ] Get credentials: `az aks get-credentials --resource-group bolttickets-rg --name bolttickets-aks`

**Common Errors:**
- `InvalidParameterValue`: Check VM sizes and node counts
- `QuotaExceeded`: Increase Azure quotas for VMs
- `NetworkNotFound`: Ensure VNet exists or use system-managed networking

#### 4. Database (PostgreSQL)
- [ ] Deploy PostgreSQL: `az deployment group create --resource-group bolttickets-rg --template-file infra/azure/database/main.bicep`
- [ ] Note server name and admin credentials
- [ ] Configure firewall rules if needed

**Common Errors:**
- `ServerNameAlreadyExists`: Choose unique server name
- `InvalidPassword`: Password must meet complexity requirements
- `FirewallRuleConflict`: Check existing firewall rules

#### 5. Redis Cache
- [ ] Deploy Redis: `az deployment group create --resource-group bolttickets-rg --template-file infra/azure/cache/main.bicep`
- [ ] Note Redis hostname and access key

**Common Errors:**
- `CacheNameAlreadyExists`: Choose unique cache name
- `InvalidSku`: Verify supported SKU combinations

#### 6. Event Hubs
- [ ] Deploy Event Hubs: `az deployment group create --resource-group bolttickets-rg --template-file infra/azure/messaging/main.bicep`
- [ ] Note namespace and connection strings

**Common Errors:**
- `NamespaceNameAlreadyExists`: Choose unique namespace name
- `InvalidCapacityUnit`: Check supported throughput units

### Application Configuration

#### Kubernetes Configuration
- [ ] Update `k8s/configmaps.yaml` with service endpoints
- [ ] Update `k8s/secrets.yaml` with credentials
- [ ] Verify ConfigMap and Secret creation

**Common Errors:**
- `ConfigMapNotFound`: Check namespace and ConfigMap names
- `SecretDecodeError`: Ensure base64 encoding for secrets

#### Azure DevOps Pipeline
- [ ] Create pipeline from `pipelines/azure-pipelines.yml`
- [ ] Configure service connections:
  - Azure Resource Manager
  - Azure Container Registry
  - Kubernetes
- [ ] Update pipeline variables
- [ ] Test pipeline run

**Common Errors:**
- `ServiceConnectionNotFound`: Verify service connection names
- `InvalidServiceConnection`: Check service connection permissions
- `DockerBuildFailed`: Verify Dockerfile paths and ACR access

### Post-Deployment Verification

#### Application Access
- [ ] Verify ingress IP: `kubectl get ingress -n bolttickets`
- [ ] Test application endpoints:
  - Frontend: http://app.localhost
  - API: http://api.localhost/health
- [ ] Check pod status: `kubectl get pods -n bolttickets`

**Common Errors:**
- `IngressNotFound`: Check ingress controller installation
- `ServiceUnavailable`: Verify service configurations
- `PodPending`: Check resource availability and node capacity

#### Monitoring Setup
- [ ] Access Grafana: `kubectl port-forward svc/grafana 3000:3000 -n bolttickets`
- [ ] Access Prometheus: `kubectl port-forward svc/prometheus 9090:9090 -n bolttickets`
- [ ] Verify metrics collection

**Common Errors:**
- `PortForwardFailed`: Check service existence and namespace
- `MetricsNotAvailable`: Verify Prometheus configuration

## AWS Deployment Checklist

### Prerequisites & Permissions

#### AWS Account Setup
- [ ] AWS account with AdministratorAccess or appropriate IAM policies
- [ ] AWS CLI installed and configured (`aws configure`)
- [ ] Terraform installed (v1.5+)
- [ ] Programmatic access keys configured

**Required IAM Permissions:**
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
        "ecr:*"
      ],
      "Resource": "*"
    }
  ]
}
```

**Common Errors:**
- `AccessDenied`: Check IAM permissions
- `InvalidAccessKeyId`: Verify AWS credentials
- `RegionNotSupported`: Choose supported AWS region

#### AWS CLI & Tools
- [ ] AWS CLI version 2.x
- [ ] kubectl installed
- [ ] eksctl installed (optional, for cluster management)

**Common Errors:**
- `Unable to locate credentials`: Check AWS configuration
- `InvalidClientTokenId`: Refresh AWS credentials

### Infrastructure Deployment

#### 1. ECR Repository
- [ ] Initialize Terraform: `cd infra/aws/registry && terraform init`
- [ ] Plan deployment: `terraform plan`
- [ ] Apply changes: `terraform apply`
- [ ] Note repository URLs

**Common Errors:**
- `RepositoryAlreadyExistsException`: Choose unique repository names
- `InvalidParameterException`: Check repository name format

#### 2. EKS Cluster
- [ ] Initialize Terraform: `cd infra/aws/eks && terraform init`
- [ ] Plan deployment: `terraform plan`
- [ ] Apply changes: `terraform apply` (15-20 minutes)
- [ ] Update kubeconfig: `aws eks update-kubeconfig --name bolttickets-eks`

**Common Errors:**
- `InsufficientCapacity`: Try different instance types or regions
- `VpcLimitExceeded`: Check VPC limits and quotas
- `NodeGroupFailed`: Verify subnet configurations

#### 3. RDS PostgreSQL
- [ ] Initialize Terraform: `cd infra/aws/database && terraform init`
- [ ] Plan deployment: `terraform plan`
- [ ] Apply changes: `terraform apply`
- [ ] Note database endpoint and credentials

**Common Errors:**
- `DBInstanceAlreadyExistsFault`: Choose unique DB instance identifier
- `InvalidParameterValue`: Check parameter values and limits
- `InsufficientDBInstanceCapacity`: Try different instance class

#### 4. ElastiCache Redis
- [ ] Initialize Terraform: `cd infra/aws/cache && terraform init`
- [ ] Plan deployment: `terraform plan`
- [ ] Apply changes: `terraform apply`
- [ ] Note Redis endpoint

**Common Errors:**
- `CacheClusterAlreadyExistsFault`: Choose unique cluster ID
- `InvalidParameterCombination`: Check parameter compatibility

#### 5. MSK Kafka
- [ ] Initialize Terraform: `cd infra/aws/messaging && terraform init`
- [ ] Plan deployment: `terraform plan`
- [ ] Apply changes: `terraform apply` (20-30 minutes)
- [ ] Note bootstrap servers and Zookeeper endpoints

**Common Errors:**
- `BadRequestException`: Check subnet configurations
- `LimitExceededException`: Check MSK cluster limits
- `ValidationException`: Verify configuration parameters

### Application Deployment

#### Kubernetes Configuration
- [ ] Update ConfigMaps with AWS service endpoints
- [ ] Update Secrets with AWS credentials
- [ ] Deploy to EKS: `kubectl apply -f k8s/`

**Common Errors:**
- `Unauthorized`: Check EKS cluster access
- `ImagePullBackOff`: Verify ECR permissions and image existence
- `Pending`: Check node capacity and resource requirements

#### CI/CD Setup (Optional)
- [ ] Configure AWS CodePipeline or GitHub Actions
- [ ] Set up ECR integration
- [ ] Configure EKS deployment

**Common Errors:**
- `AccessDenied`: Check IAM roles for CI/CD service
- `InvalidSource`: Verify source repository access

### Post-Deployment Verification

#### Application Testing
- [ ] Verify pod status: `kubectl get pods`
- [ ] Check service endpoints
- [ ] Test application functionality

**Common Errors:**
- `CrashLoopBackOff`: Check application logs and configuration
- `ImagePullBackOff`: Verify ECR access and image tags
- `Pending`: Check resource quotas and limits

#### Monitoring & Logging
- [ ] Set up CloudWatch Container Insights
- [ ] Configure CloudWatch Logs
- [ ] Set up CloudWatch Alarms

**Common Errors:**
- `InvalidParameterException`: Check log group configurations
- `LimitExceededException`: Check CloudWatch limits

## General Troubleshooting

### Network Issues
- [ ] Verify security groups allow required traffic
- [ ] Check network ACLs and route tables
- [ ] Validate DNS resolution

### Permission Issues
- [ ] Review IAM policies and roles
- [ ] Check service-linked roles
- [ ] Verify resource-based policies

### Resource Limits
- [ ] Check service quotas and limits
- [ ] Monitor resource utilization
- [ ] Implement auto-scaling where appropriate

### Cost Monitoring
- [ ] Set up billing alerts
- [ ] Monitor resource usage
- [ ] Implement cost allocation tags

## Rollback Procedures

### Azure Rollback
- [ ] Scale down problematic deployment: `kubectl scale deployment api --replicas=0 -n bolttickets`
- [ ] Rollback to previous version: `kubectl rollout undo deployment/api -n bolttickets`
- [ ] Verify rollback success

### AWS Rollback
- [ ] Update deployment with previous image tag
- [ ] Monitor rollback progress
- [ ] Verify application functionality

## Support Contacts

- Azure Support: https://azure.microsoft.com/support
- AWS Support: https://aws.amazon.com/support
- Development Team: [team@company.com]