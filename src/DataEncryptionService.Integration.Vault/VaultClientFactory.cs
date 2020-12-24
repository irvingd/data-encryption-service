using System;
using DataEncryptionService.Configuration;
using Microsoft.Extensions.Logging;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods.UserPass;

namespace DataEncryptionService.Integration.Vault
{
    public class VaultClientFactory : IVaultClientFactory
    {
        private readonly ILogger _log;

        public VaultClientFactory(ILogger<VaultClientFactory> log)
        {
            _log = log;
        }

        public (IVaultClient, string) CreateClient(VaultServiceConfiguration config)
        {
            IVaultClient client = null;
            string errorMessage = null;
            bool authConfigured = false;
            IAuthMethodInfo authMethod = null;

            if (!string.IsNullOrEmpty(config.ServiceUrl))
            {
                string configAuthMethod = (string.IsNullOrWhiteSpace(config.AuthMethod) ? string.Empty : config.AuthMethod).Trim().ToLower();
                switch (configAuthMethod)
                {
                    case "token":
                        {
                            config.Parameters.TryGetValue(WellKnownConstants.Vault.Configuration.Token, out string authToken);
                            if (!string.IsNullOrEmpty(authToken))
                            {
                                authMethod = new TokenAuthMethodInfo(authToken);
                                authConfigured = true;
                            }
                            else
                            {
                                errorMessage = "Token is not configured.";
                            }
                        }
                        break;

                    case "userpass":
                        {
                            config.Parameters.TryGetValue(WellKnownConstants.Vault.Configuration.UserName, out string userName);
                            config.Parameters.TryGetValue(WellKnownConstants.Vault.Configuration.Password, out string password);
                            if (!string.IsNullOrEmpty(userName))
                            {
                                authMethod = new UserPassAuthMethodInfo(userName, password);
                                authConfigured = true;
                            }
                            else
                            {
                                errorMessage = "User name is not configured.";
                            }
                        }
                        break;

                    case "approle":
                        {
                            config.Parameters.TryGetValue(WellKnownConstants.Vault.Configuration.RoleId, out string roleId);
                            config.Parameters.TryGetValue(WellKnownConstants.Vault.Configuration.SecretId, out string secretId);
                            // The secret Id may be null or empty
                            if (!string.IsNullOrEmpty(roleId))
                            {
                                authMethod = new AppRoleAuthMethodInfo(roleId, secretId);
                                authConfigured = true;
                            }
                            else
                            {
                                errorMessage = "The role ID is not configured.";
                            }
                        }
                        break;

                    case "":
                        _log.LogError($"No authorization method defined in the configuration.");
                        break;

                    default:
                        _log.LogError($"Authorization method [{config.AuthMethod}] not implemented.");
                        break;
                }

                if (authConfigured)
                {
                    var vaultClientSettings = new VaultClientSettings(config.ServiceUrl, authMethod);
                    if (config.ApiTimeout > 0)
                    {
                        vaultClientSettings.VaultServiceTimeout = TimeSpan.FromSeconds(config.ApiTimeout);
                    }

                    client = new VaultClient(vaultClientSettings);
                }
                else
                {
                    if (errorMessage is null && authMethod is null)
                    {
                        errorMessage = "A valid and/or supported authorization method is not defined in the configuration.";
                    }
                }
            }
            else
            {
                errorMessage = "The Vault service URL is not configured.";
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                _log.LogError($"Failed to create Vault Client: {errorMessage}");
            }

            return (client, errorMessage);
        }
    }
}