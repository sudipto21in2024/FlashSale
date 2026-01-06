# Terraform Tutorial for BoltTickets AWS Infrastructure

This tutorial explains the Terraform code implemented for deploying BoltTickets infrastructure on AWS. Each module is broken down with explanations of resources, variables, and outputs.

## Prerequisites

- AWS CLI configured with appropriate permissions
- Terraform v1.5+ installed
- Basic understanding of AWS services

## Project Structure

```
infra/aws/
├── registry/     # ECR repositories
├── eks/         # EKS cluster
├── database/    # RDS PostgreSQL
├── cache/       # ElastiCache Redis
└── messaging/   # MSK Kafka
```

## 1. ECR Registry Module

**File**: `infra/aws/registry/main.tf`

### Purpose
Creates Amazon Elastic Container Registry repositories for storing Docker images.

### Key Components

```hcl
terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}
```
- Specifies AWS provider version requirement

```hcl
variable "repository_names" {
  description = "List of ECR repository names"
  type        = list(string)
  default     = ["bolttickets/api", "bolttickets/worker", "bolttickets/frontend"]
}
```
- Defines input variable for repository names
- Uses list type with default values

```hcl
resource "aws_ecr_repository" "repositories" {
  for_each = toset(var.repository_names)

  name                 = each.value
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }
}
```
- Creates ECR repository for each name in the list
- `for_each = toset(var.repository_names)` iterates over the list
- `each.value` refers to current repository name
- Enables image scanning for security

```hcl
output "repository_urls" {
  description = "ECR repository URLs"
  value       = { for repo in aws_ecr_repository.repositories : repo.name => repo.repository_url }
}
```
- Outputs map of repository names to URLs
- Uses `for` expression to transform resource attributes

### Usage
```bash
cd infra/aws/registry
terraform init
terraform plan
terraform apply
```

## 2. EKS Cluster Module

**File**: `infra/aws/eks/main.tf`

### Purpose
Creates Amazon EKS cluster with managed node groups.

### Key Components

```hcl
resource "aws_eks_cluster" "main" {
  name     = var.cluster_name
  version  = var.kubernetes_version
  role_arn = aws_iam_role.eks_cluster.arn

  vpc_config {
    subnet_ids = aws_subnet.private[*].id
  }

  depends_on = [
    aws_iam_role_policy_attachment.eks_cluster,
  ]
}
```
- Creates EKS cluster with specified version
- Uses IAM role for cluster permissions
- Configures VPC subnets for worker nodes
- `depends_on` ensures IAM role is created first

```hcl
resource "aws_eks_node_group" "main" {
  cluster_name    = aws_eks_cluster.main.name
  node_group_name = var.node_group_name
  node_role_arn   = aws_iam_role.eks_nodes.arn
  subnet_ids      = aws_subnet.private[*].id
  instance_types  = var.instance_types

  scaling_config {
    desired_size = var.desired_capacity
    min_size     = var.min_size
    max_size     = var.max_size
  }

  depends_on = [
    aws_iam_role_policy_attachment.eks_nodes,
  ]
}
```
- Creates managed node group
- Configures auto-scaling parameters
- Uses `[*]` splat operator to get all subnet IDs

```hcl
resource "aws_iam_role" "eks_cluster" {
  name = "${var.cluster_name}-cluster-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "eks.amazonaws.com"
      }
    }]
  })
}
```
- Creates IAM role for EKS cluster
- Uses `assume_role_policy` for cross-service access

```hcl
resource "aws_iam_role_policy_attachment" "eks_cluster" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSClusterPolicy"
  role       = aws_iam_role.eks_cluster.name
}
```
- Attaches AWS managed policy to cluster role
- Required for EKS cluster operation

### VPC Configuration
```hcl
resource "aws_vpc" "main" {
  cidr_block = "10.0.0.0/16"
}

resource "aws_subnet" "private" {
  count             = 3
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.${count.index}.0/24"
  availability_zone = data.aws_availability_zones.available.names[count.index]
}
```
- Creates VPC with /16 CIDR
- Creates 3 private subnets across availability zones
- Uses `count` meta-argument for multiple resources
- `data.aws_availability_zones.available` fetches AZ information

## 3. RDS Database Module

**File**: `infra/aws/database/main.tf`

### Purpose
Creates PostgreSQL database instance with security groups.

### Key Components

```hcl
resource "aws_db_instance" "postgres" {
  identifier             = "bolttickets-postgres"
  engine                 = "postgres"
  engine_version         = var.engine_version
  instance_class         = var.instance_class
  allocated_storage      = var.allocated_storage
  db_name                = var.db_name
  username               = var.db_username
  password               = var.db_password
  publicly_accessible    = false
  vpc_security_group_ids = [aws_security_group.rds.id]
  db_subnet_group_name   = aws_db_subnet_group.main.name
  skip_final_snapshot    = true
}
```
- Creates RDS PostgreSQL instance
- Configures security groups and subnet groups
- `skip_final_snapshot = true` for development (not recommended for production)

```hcl
resource "aws_db_subnet_group" "main" {
  name       = "bolttickets-db-subnet"
  subnet_ids = var.private_subnet_ids

  tags = {
    Name = "bolttickets-db-subnet"
  }
}
```
- Creates DB subnet group for multi-AZ deployment
- Associates with private subnets

```hcl
resource "aws_security_group" "rds" {
  name_prefix = "bolttickets-rds-"
  vpc_id      = var.vpc_id

  ingress {
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = var.allowed_cidr_blocks
  }

  tags = {
    Name = "bolttickets-rds-sg"
  }
}
```
- Creates security group for database access
- Allows PostgreSQL port (5432) from specified CIDR blocks

## 4. ElastiCache Redis Module

**File**: `infra/aws/cache/main.tf`

### Purpose
Creates Redis cluster for caching with security groups.

### Key Components

```hcl
resource "aws_elasticache_cluster" "redis" {
  cluster_id           = var.cluster_id
  engine               = "redis"
  node_type            = var.node_type
  num_cache_nodes      = var.num_cache_nodes
  engine_version       = var.engine_version
  port                 = 6379
  security_group_ids   = [aws_security_group.redis.id]
  subnet_group_name    = aws_elasticache_subnet_group.main.name
}
```
- Creates ElastiCache Redis cluster
- Single-node configuration (num_cache_nodes = 1)
- Associates with security group and subnet group

```hcl
resource "aws_elasticache_subnet_group" "main" {
  name       = "bolttickets-redis-subnet"
  subnet_ids = var.private_subnet_ids

  tags = {
    Name = "bolttickets-redis-subnet"
  }
}
```
- Creates cache subnet group for Redis placement

## 5. MSK Kafka Module

**File**: `infra/aws/messaging/main.tf`

### Purpose
Creates Amazon Managed Streaming for Kafka with configuration.

### Key Components

```hcl
resource "aws_msk_cluster" "main" {
  cluster_name           = var.cluster_name
  kafka_version          = var.kafka_version
  number_of_broker_nodes = var.number_of_broker_nodes

  broker_node_group_info {
    instance_type   = var.instance_type
    client_subnets  = var.private_subnet_ids
    security_groups = [aws_security_group.msk.id]

    storage_info {
      ebs_storage_info {
        volume_size = 100
      }
    }
  }

  configuration_info {
    arn      = aws_msk_configuration.main.arn
    revision = aws_msk_configuration.main.latest_revision
  }
}
```
- Creates MSK cluster with specified broker configuration
- Uses custom configuration for topic settings
- Configures EBS storage per broker

```hcl
resource "aws_msk_configuration" "main" {
  kafka_versions = [var.kafka_version]
  name           = "bolttickets-msk-config"

  server_properties = <<PROPERTIES
auto.create.topics.enable=true
default.replication.factor=2
min.insync.replicas=1
num.io.threads=8
num.network.threads=5
num.partitions=3
num.replica.fetchers=2
replica.lag.time.max.ms=30000
socket.receive.buffer.bytes=102400
socket.request.max.bytes=104857600
socket.send.buffer.bytes=102400
unclean.leader.election.enable=true
zookeeper.connection.timeout.ms=1000
PROPERTIES
}
```
- Creates custom MSK configuration
- Uses heredoc syntax for multi-line properties
- Configures Kafka broker settings

```hcl
resource "aws_security_group" "msk" {
  name_prefix = "bolttickets-msk-"
  vpc_id      = var.vpc_id

  ingress {
    from_port   = 9092
    to_port     = 9092
    protocol    = "tcp"
    cidr_blocks = var.allowed_cidr_blocks
  }

  ingress {
    from_port   = 9094
    to_port     = 9094
    protocol    = "tcp"
    cidr_blocks = var.allowed_cidr_blocks
  }
}
```
- Creates security group for MSK access
- Allows Kafka ports (9092 for plaintext, 9094 for TLS)

## Common Terraform Patterns Used

### 1. Variables and Locals
```hcl
variable "cluster_name" {
  description = "EKS cluster name"
  type        = string
  default     = "bolttickets-eks"
}

locals {
  common_tags = {
    Project     = "BoltTickets"
    Environment = "Production"
  }
}
```

### 2. Data Sources
```hcl
data "aws_availability_zones" "available" {
  state = "available"
}
```

### 3. Resource Dependencies
```hcl
depends_on = [
  aws_iam_role_policy_attachment.eks_cluster,
]
```

### 4. Output Values
```hcl
output "cluster_endpoint" {
  description = "EKS cluster endpoint"
  value       = aws_eks_cluster.main.endpoint
}
```

### 5. Resource Iteration
```hcl
resource "aws_ecr_repository" "repositories" {
  for_each = toset(var.repository_names)
  # ...
}
```

## Deployment Workflow

1. **Initialize**: `terraform init`
2. **Plan**: `terraform plan` (review changes)
3. **Apply**: `terraform apply` (deploy infrastructure)
4. **Destroy**: `terraform destroy` (cleanup, when needed)

## State Management

- Terraform state is stored locally by default
- For team collaboration, use remote state (S3 + DynamoDB)
- Always backup state files before major changes

## Best Practices Implemented

- **Modular Structure**: Separate modules for each service
- **Variables**: Configurable parameters for different environments
- **Security Groups**: Least privilege access control
- **Tags**: Resource tagging for cost tracking
- **Outputs**: Expose important resource information
- **Dependencies**: Proper resource ordering

## Troubleshooting

### Common Issues
- **Dependency Errors**: Check `depends_on` and resource references
- **IAM Issues**: Verify AWS credentials and permissions
- **Quota Limits**: Check AWS service limits and request increases
- **State Locks**: Use `terraform force-unlock` if state is locked

### Debugging
- Use `terraform plan -out=tfplan` to save execution plan
- Use `terraform show tfplan` to inspect planned changes
- Enable verbose logging: `TF_LOG=DEBUG terraform apply`

This Terraform implementation provides a complete, production-ready infrastructure foundation for the BoltTickets application on AWS.