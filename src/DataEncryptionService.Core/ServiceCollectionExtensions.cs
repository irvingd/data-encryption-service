using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataEncryptionService.Configuration;
using DataEncryptionService.Core;
using DataEncryptionService.Core.CryptoEngines;
using DataEncryptionService.Core.CryptoEngines.dotNetCapi;
using DataEncryptionService.Core.Storage;
using DataEncryptionService.CryptoEngines;
using DataEncryptionService.Storage;
using DataEncryptionService.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataEncryptionService
{
    public static class ServiceCollectionExtensions
    {
        private static char[] delimiterChars = { ' ', ',', '|' };

        public static IServiceCollection AddDataEncryptionServices(this IServiceCollection services, string configFileName = "appsettings.json", Stream configStream = null)
        {
            services.AddSingleton<IStringHasher, StringHasher>();

            services.AddSingleton<ICryptoEngineFactory, CryptoEngineFactory>();
            services.AddSingleton<ICryptographicEngine, AesCapiCryptoEngine>();
            services.AddSingleton<ICryptographicEngine, TripleDesCapiCryptoEngine>();
            
            services.AddSingleton<IStorageProviderFactory, StorageProviderFactory>();

            services.AddSingleton<IDataEncryptionManager, DataEncryptionManager>();

            DataEncryptionServiceConfiguration serviceConfiguration =  LoadConfiguration(configFileName, configStream);
            serviceConfiguration ??= new DataEncryptionServiceConfiguration();
            services.AddSingleton(serviceConfiguration);
            services.AddSingleton(serviceConfiguration.Storage);
            services.AddSingleton(serviceConfiguration.Telemetry);
            services.AddSingleton(serviceConfiguration.VaultService);
            services.AddSingleton(serviceConfiguration.Policies);

            services.AddTelemetryClient();

            return services;
        }

        private static DataEncryptionServiceConfiguration LoadConfiguration(string configFileName, Stream configStream)
        {
            // Load the configuration
            IConfigurationBuilder configBuilder = new ConfigurationBuilder().AddJsonFile(configFileName, optional: true, reloadOnChange: true);
            if (null != configStream)
            {
                configBuilder.AddJsonStream(configStream);
            }

            IConfiguration config = configBuilder.Build();
            var svcConfig = config.Get<DataEncryptionServiceConfiguration>();
            if (null != svcConfig)
            {
                // NOTE: .NET Core 5.0 RC2 cannot read a config section that is Dictionary<TKey,TValue> if TKey
                //       is anything other than a string. As a temporary workarount until this is fixed:
                //          TKey for the ActiveKeys dictionary is supposed to be a GUID - read all the pairs and re-add the to a
                //          new dictionary using the expected formad Guid.ToString("N") so other parts of the code can search assuming this format.
                var dict = new Dictionary<string, string>();
                foreach (var item in svcConfig.Encryption.ActiveKeys)
                {
                    Guid key;
                    if (Guid.TryParse(item.Key, out key))
                    {
                        dict.Add(key.ToString("N"), item.Value);
                    }
                }
                svcConfig.Encryption.ActiveKeys = dict;
            }
            else
            {
                // Create default
                svcConfig = new DataEncryptionServiceConfiguration();
            }

            // After data has been loaded from the config stream or file, load the values in the ENVIRONMENT.
            // RULE 1: Environment variables override anything specified in the config file or config stream
            // RULE 2: Only CERTAIN variables can be overriden through environment variables
            LoadConfigFromEnvironmentVariables(svcConfig);

            return svcConfig;
        }

        private static void LoadConfigFromEnvironmentVariables(DataEncryptionServiceConfiguration config)
        {
            var allVars = Environment.GetEnvironmentVariables();
            var keys = allVars.Keys.Cast<string>();

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Set the Storage properties
            Guid guidValue;
            if (Guid.TryParse(GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.StorageProvider), out guidValue))
            {
                config.Storage.StorageProvider = guidValue;
            }
            var value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.StorageMongoDbConnectiongString);
            config.Storage.MongoDbConnectionString = value == null ? config.Storage.MongoDbConnectionString : value;

            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.StorageMssqlConnectiongString);
            config.Storage.MssqlConnectionString = value == null ? config.Storage.MssqlConnectionString : value;

            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.StorageMssqlConnectiongString);
            config.Storage.MySqlConnectionString = value == null ? config.Storage.MySqlConnectionString : value;

            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.StoragePostgresqlConnectiongString);
            config.Storage.PostgresqlConnectionString = value == null ? config.Storage.PostgresqlConnectionString : value;
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Set the Encryption properties
            if (Guid.TryParse(GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.EncryptionEngineID), out guidValue))
            {
                config.Encryption.DefaultEngine = guidValue;
            }
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Set the VaultService properties
            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.VaultServerUrl);
            if (!string.IsNullOrEmpty(value))
            {
                config.VaultService.ServiceUrl = value;
            }

            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.VaultAuthMethod);
            if (!string.IsNullOrEmpty(value))
            {
                config.VaultService.AuthMethod = value;
            }

            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.VaultAuthToken);
            SetParameterValue(config.VaultService.Parameters, WellKnownConstants.Vault.Configuration.Token, value);

            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.VaultAuthUserName);
            SetParameterValue(config.VaultService.Parameters, WellKnownConstants.Vault.Configuration.UserName, value);
            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.VaultAuthUserPass);
            SetParameterValue(config.VaultService.Parameters, WellKnownConstants.Vault.Configuration.Password, value);

            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.VaultAuthAppRoleId);
            SetParameterValue(config.VaultService.Parameters, WellKnownConstants.Vault.Configuration.RoleId, value);
            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.VaultAuthAppSecretId);
            SetParameterValue(config.VaultService.Parameters, WellKnownConstants.Vault.Configuration.SecretId, value);
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Set the Vault encryption engine properties
            ServiceConfigEncryptionEngine vaultEngine = config.Encryption.EngineConfigurations.FirstOrDefault(x => x.EngineId == WellKnownConstants.Vault.CryptoEngineUUID);
            if (null == vaultEngine)
            {
                vaultEngine = new ServiceConfigEncryptionEngine() { EngineId = WellKnownConstants.Vault.CryptoEngineUUID };
                config.Encryption.EngineConfigurations.Add(vaultEngine);
            }
            
            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.VaultEncryptionKey);
            SetParameterValue(vaultEngine.Parameters, WellKnownConstants.Vault.Configuration.DefaultKeyName, value);
            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.VaultEncryptionMountPoint);
            SetParameterValue(vaultEngine.Parameters, WellKnownConstants.Vault.Configuration.DefaultMountPoint, value);
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Set the Policies properties
            value = GetEnvironmentVariableValue(allVars, keys, DesEnvironmentVariables.PoliciesApiRequiredTokenPolicies);
            if (!string.IsNullOrEmpty(value))
            {
                config.Policies.API.RequiredTokenPolicies.Clear();
                config.Policies.API.RequiredTokenPolicies.AddRange(value.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));
            }
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }

        private static void SetParameterValue(Dictionary<string, string> parameters, string paramName, string paramValue)
        {
            if (null != paramValue)
            {
                parameters.Remove(paramName);
                parameters.Add(paramName, paramValue);
            }
        }

        private static string GetEnvironmentVariableValue(System.Collections.IDictionary allVars, IEnumerable<string> keys, string desVariable)
        {
            var key = keys.FirstOrDefault(x => x.Equals(desVariable, StringComparison.InvariantCultureIgnoreCase));
            if (null != key)
            {
                return allVars[key].ToString();
            }

            return null;
        }
    }
}
