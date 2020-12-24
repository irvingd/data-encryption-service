using Microsoft.AspNetCore.Authentication;

namespace DataEncryptionService.WebApi.Security
{
    public class DataEncryptionServiceTokenAuthOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScemeName = "HashicorpVaultToken";
        public string TokenHeaderName { get; set; } = "X-Vault-Token";
    }
}