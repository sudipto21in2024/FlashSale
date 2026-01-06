terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

variable "repository_names" {
  description = "List of ECR repository names"
  type        = list(string)
  default     = ["bolttickets/api", "bolttickets/worker", "bolttickets/frontend"]
}

resource "aws_ecr_repository" "repositories" {
  for_each = toset(var.repository_names)

  name                 = each.value
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }
}

output "repository_urls" {
  description = "ECR repository URLs"
  value       = { for repo in aws_ecr_repository.repositories : repo.name => repo.repository_url }
}