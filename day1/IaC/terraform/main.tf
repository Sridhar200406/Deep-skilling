terraform {
  required_version = ">= 1.5.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>4.0"
    }
  }
  # Backend configuration (optional). Uncomment and configure if using remote state.
  # backend "azurerm" {}
}

provider "azurerm" {
  features {}
}

# Resource Group
module "resource_group" {
  source   = "./modules/resource-group"
  name     = var.resource_group_name
  location = var.location
}

# SQL Database
module "sql_database" {
  source               = "./modules/sql-database"
  resource_group_name  = module.resource_group.name
  location             = var.location
  sql_admin_username   = var.sql_admin_username
  sql_admin_password   = var.sql_admin_password
  sql_sku_name         = var.sql_sku_name
  sql_version          = var.sql_version
}

# Storage
module "storage" {
  source               = "./modules/storage"
  resource_group_name  = module.resource_group.name
  location             = var.location
  storage_account_name = var.storage_account_name
  container_name       = var.storage_container_name
}

# Key Vault
module "key_vault" {
  source               = "./modules/key-vault"
  resource_group_name  = module.resource_group.name
  location             = var.location
  vault_name           = var.key_vault_name
  tenant_id            = var.tenant_id
  access_object_id     = var.access_object_id
}

# Container Registry
module "container_registry" {
  source               = "./modules/container-registry"
  resource_group_name  = module.resource_group.name
  location             = var.location
  registry_name        = var.container_registry_name
  sku                  = var.container_registry_sku
}

# Container Apps
module "container_apps" {
  source               = "./modules/container-apps"
  resource_group_name  = module.resource_group.name
  location             = var.location
  environment_name     = var.environment_name
  container_registry_url = module.container_registry.login_server
  # Additional inputs for each app can be added here.
}

# Service Bus
module "service_bus" {
  source               = "./modules/service-bus"
  resource_group_name  = module.resource_group.name
  location             = var.location
  sb_namespace_name    = var.service_bus_namespace_name
  sb_sku               = var.service_bus_sku
}

# Monitoring
module "monitoring" {
  source               = "./modules/monitoring"
  resource_group_name  = module.resource_group.name
  location             = var.location
  workspace_name       = var.log_analytics_workspace_name
}
