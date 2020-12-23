using DataEncryptionService.CryptoEngines;
using DataEncryptionService.Integration.Vault.CryptoEngine;
using Microsoft.Extensions.DependencyInjection;

namespace DataEncryptionService.Integration.Vault
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataEncryptionServiceVaultIntegration(this IServiceCollection services)
        {
            services.AddSingleton<ICryptographicEngine, VaultTransitCryptoEngine>();
            services.AddSingleton<IVaultClientFactory, VaultClientFactory>();

            return services;
        }
    }
}
