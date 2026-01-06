terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

variable "cluster_name" {
  description = "MSK cluster name"
  type        = string
  default     = "bolttickets-msk"
}

variable "kafka_version" {
  description = "Kafka version"
  type        = string
  default     = "2.8.1"
}

variable "number_of_broker_nodes" {
  description = "Number of broker nodes"
  type        = number
  default     = 2
}

variable "instance_type" {
  description = "Broker instance type"
  type        = string
  default     = "kafka.t3.small"
}

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

  encryption_info {
    encryption_in_transit {
      client_broker = "TLS"
      in_cluster    = true
    }
  }

  tags = {
    Name = "bolttickets-msk"
  }
}

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

  ingress {
    from_port   = 2181
    to_port     = 2181
    protocol    = "tcp"
    cidr_blocks = var.allowed_cidr_blocks
  }

  ingress {
    from_port   = 2888
    to_port     = 2888
    protocol    = "tcp"
    cidr_blocks = var.allowed_cidr_blocks
  }

  ingress {
    from_port   = 3888
    to_port     = 3888
    protocol    = "tcp"
    cidr_blocks = var.allowed_cidr_blocks
  }

  tags = {
    Name = "bolttickets-msk-sg"
  }
}

variable "vpc_id" {
  description = "VPC ID"
  type        = string
}

variable "private_subnet_ids" {
  description = "Private subnet IDs"
  type        = list(string)
}

variable "allowed_cidr_blocks" {
  description = "Allowed CIDR blocks for MSK access"
  type        = list(string)
  default     = ["10.0.0.0/8"]
}

output "cluster_arn" {
  description = "MSK cluster ARN"
  value       = aws_msk_cluster.main.arn
}

output "bootstrap_brokers_tls" {
  description = "Bootstrap brokers TLS connection string"
  value       = aws_msk_cluster.main.bootstrap_brokers_tls
}

output "zookeeper_connect_string" {
  description = "Zookeeper connect string"
  value       = aws_msk_cluster.main.zookeeper_connect_string
}