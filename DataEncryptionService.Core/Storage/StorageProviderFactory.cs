using System;
using System.Collections.Generic;
using System.Linq;
using DataEncryptionService.Configuration;
using DataEncryptionService.Storage;
using Microsoft.Extensions.Logging;

namespace DataEncryptionService.Core.Storage
{
    public class StorageProviderFactory : IStorageProviderFactory
    {
        private readonly ILogger _log;
        private readonly ServiceConfigStorage _storageConfig;
        private readonly IEnumerable<IStorageProvider> _providers;

        public StorageProviderFactory(DataEncryptionServiceConfiguration config, IEnumerable<IStorageProvider> providers, ILogger<StorageProviderFactory> log)
        {
            _log = log;
            _storageConfig = config.Storage;

            if (!providers.Any(s => s.ProviderId == _storageConfig.StorageProvider))
            {
                _log.LogError($"The configured storage provider [{GetProviderInfo(_storageConfig.StorageProvider)}] is not available in the configured services. Perhaps a reference to an integration package is missing.");
            }

            // Only add the storage providers that are fully configured and can be used. Each implementation decides
            // what it requires to consider itself in a "configured" state
            _providers = providers.Where(item => item.IsConfigured);

            if (_providers.Any(s => s.ProviderId == _storageConfig.StorageProvider))
            {
                _log.LogInformation($"Secure storage configured for: [{GetProviderInfo(_storageConfig.StorageProvider)}]");
            }
        }

        public IStorageProvider CreateProvider()
        {
            // Get the configured storage provider
            IStorageProvider provider = _providers
                                            .Where(s => s.ProviderId == _storageConfig.StorageProvider)
                                            .FirstOrDefault();
            if (null == provider)
            {
                _log.LogError($"The registered storage provider [{GetProviderInfo(_storageConfig.StorageProvider)}] is not fully configured and cannot be used. Verify the required configuration values.");
            }

            return provider;
        }

        private static string GetProviderInfo(Guid providerId)
        {
            return providerId switch
            {
                var g when g == WellKnownConstants.MongoDB.StorageProviderUUID => $"{WellKnownConstants.MongoDB.Name} ({WellKnownConstants.MongoDB.StorageProviderUUID})",
                var g when g == WellKnownConstants.MySql.StorageProviderUUID => $"{WellKnownConstants.MySql.Name} ({WellKnownConstants.MySql.StorageProviderUUID})",
                var g when g == WellKnownConstants.MSSQL.StorageProviderUUID => $"{WellKnownConstants.MSSQL.Name} ({WellKnownConstants.MSSQL.StorageProviderUUID})",
                var g when g == WellKnownConstants.Postgresql.StorageProviderUUID => $"{WellKnownConstants.Postgresql.Name} ({WellKnownConstants.Postgresql.StorageProviderUUID})",
                _ => providerId.ToString(),
            };
        }
    }
}