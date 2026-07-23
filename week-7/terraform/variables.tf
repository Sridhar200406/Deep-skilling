variable "environment" {
  description = "Deployment environment (dev or prod)"
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure region for all resources"
  type        = string
  default     = "East US"
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

# SQL variables
variable "sql_admin_username" {
  description = "Admin username for Azure SQL Server"
  type        = string
}
variable "sql_admin_password" {
  description = "Admin password for Azure SQL Server (should be sourced from Key Vault)"
  type        = string
  sensitive   = true
}
variable "sql_sku_name" {
  description = "SKU for Azure SQL Server (e.g., GP_Gen5_2)"
  type        = string
  default     = "GP_Gen5_2"
}
variable "sql_version" {
  description = "SQL Server version"
  type        = string
  default     = "12.0"
}

# Storage variables
variable "storage_account_name" {
  description = "Storage account name (must be globally unique)"
  type        = string
}
variable "storage_container_name" {
  description = "Blob container name"
  type        = string
  default     = "employee-docs"
}

# Key Vault variables
variable "key_vault_name" {
  description = "Key Vault name"
  type        = string
}
variable "tenant_id" {
  description = "Azure AD tenant ID"
  type        = string
}
variable "access_object_id" {
  description = "Object ID (user or managed identity) that needs access to the vault"
  type        = string
}

# Container Registry variables
variable "container_registry_name" {
  description = "Container Registry name (must be globally unique)"
  type        = string
}
variable "container_registry_sku" {
  description = "SKU for the registry"
  type        = string
  default     = "Basic"
}

# Container Apps variables
variable "environment_name" {
  description = "Name for the Container Apps environment"
  type        = string
}

# Service Bus variables
variable "service_bus_namespace_name" {
  description = "Service Bus namespace name"
  type        = string
}
variable "service_bus_sku" {
  description = "SKU for Service Bus (Standard, Basic)"
  type        = string
  default     = "Standard"
}

# Monitoring variables
variable "log_analytics_workspace_name" {
  description = "Log Analytics workspace name"
  type        = string
}
