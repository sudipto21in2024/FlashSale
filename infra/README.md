# Infrastructure as Code

This directory contains infrastructure definitions for deploying BoltTickets to Azure and AWS cloud platforms.

## Directory Structure

```
infra/
├── azure/                 # Azure infrastructure
│   ├── aks/              # AKS cluster configuration
│   ├── database/         # Azure Database for PostgreSQL
│   ├── cache/            # Azure Cache for Redis
│   ├── messaging/        # Azure Event Hubs (Kafka)
│   └── registry/         # Azure Container Registry
├── aws/                   # AWS infrastructure
│   ├── eks/              # EKS cluster configuration
│   ├── database/         # RDS PostgreSQL
│   ├── cache/            # ElastiCache Redis
│   ├── messaging/        # MSK (Kafka)
│   └── registry/         # ECR
└── README.md
```

## Prerequisites

### Azure
- Azure CLI installed
- Terraform or Bicep CLI
- Azure subscription with required permissions

### AWS
- AWS CLI installed
- Terraform
- AWS account with required permissions

## Deployment Order

1. Container Registry
2. Database
3. Cache
4. Messaging
5. Kubernetes Cluster
6. Application Deployment

## Security Notes

- Use Azure Key Vault or AWS Secrets Manager for secrets
- Implement network security groups and security groups
- Enable encryption at rest and in transit
- Use managed identities/service accounts where possible

## Cost Optimization

- Use spot instances for non-critical workloads
- Implement auto-scaling
- Monitor resource usage and adjust sizing
- Use reserved instances for predictable workloads