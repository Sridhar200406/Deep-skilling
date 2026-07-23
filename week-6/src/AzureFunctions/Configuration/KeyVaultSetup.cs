using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EmployeeManagement.AzureFunctions.Configuration;

/// <summary>
/// Wires Azure Key Vault into the Functions configuration pipeline.
/// Works identically to the Employee API — secrets loaded before DI resolves them.
///
/// In Azure:   Uses Managed Identity automatically (no credentials stored anywhere).
/// Locally:    Uses Azure CLI credential (az login) or env vars.
///
/// Secret naming: double-dash (--) replaces colon (:) hierarchy separator.
///   "ConnectionStrings:DefaultConnection" → "ConnectionStrings--DefaultConnection"
/// </summary>
public static class KeyVaultSetup
{
    public static IHostBuilder AddAzureKeyVault(this IHostBuilder builder)
    {
        return builder.ConfigureAppConfiguration((context, config) =>
        {
            var builtConfig = config.Build();
            var vaultUri = builtConfig["AzureKeyVault:VaultUri"];

            if (string.IsNullOrEmpty(vaultUri))
            {
                Console.WriteLine("[Functions KeyVault] VaultUri not set — skipping Key Vault.");
                return;
            }

            Console.WriteLine($"[Functions KeyVault] Loading secrets from: {vaultUri}");

            // DefaultAzureCredential tries in order:
            //   1. Environment variables (CI/CD service principal)
            //   2. Managed Identity (Azure Function App)
            //   3. Visual Studio / VS Code credential
            //   4. Azure CLI (local dev: az login)
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeVisualStudioCredential = false,
                ExcludeAzureCliCredential = false,
                ExcludeManagedIdentityCredential = false
            });

            config.AddAzureKeyVault(new Uri(vaultUri), credential);
            Console.WriteLine("[Functions KeyVault] Key Vault configuration loaded.");
        });
    }
}
