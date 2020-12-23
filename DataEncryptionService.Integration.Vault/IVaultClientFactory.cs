using DataEncryptionService.Configuration;
using VaultSharp;

namespace DataEncryptionService.Integration.Vault
{
    public interface IVaultClientFactory
    {
        (IVaultClient, string) CreateClient(VaultServiceConfiguration serviceConfig);
    }
}