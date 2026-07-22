using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace EmployeeService.Infrastructure.Azure;

/// <summary>
/// Extension methods to integrate Azure Key Vault into the ASP.NET Core configuration pipeline.
/// Secrets stored in Key Vault override values in appsettings files.
/// 
/// Key Vault Secret Naming Convention:
///   ASP.NET Core config key "ConnectionStrings:DefaultConnection"
///   → Key Vault secret name: "ConnectionStrings--DefaultConnection"
///   (double-dash replaces the colon separator)
/// </summary>
public static class KeyVaultConfiguration
{
    public static IConfigurationBuilder AddAzureKeyVaultIfConfigured(
        this IConfigurationBuilder builder,
        IConfiguration configuration)
    {
        var vaultUri = configuration["AzureKeyVault:VaultUri"];

        if (string.IsNullOrEmpty(vaultUri))
        {
            Console.WriteLine("[KeyVault] AzureKeyVault:VaultUri is not set. Skipping Key Vault integration.");
            return builder;
        }

        Console.WriteLine($"[KeyVault] Connecting to Azure Key Vault: {vaultUri}");

        // DefaultAzureCredential automatically tries:
        //   1. Environment variables (AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TENANT_ID)
        //   2. Managed Identity (when running on Azure App Service / Azure VM)
        //   3. Visual Studio credential
        //   4. Azure CLI credential (for local development: az login)
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeVisualStudioCredential = false,
            ExcludeAzureCliCredential = false,
            ExcludeManagedIdentityCredential = false
        });

        builder.AddAzureKeyVault(new Uri(vaultUri), credential);

        Console.WriteLine("[KeyVault] Azure Key Vault configuration added successfully.");
        return builder;
    }
}
