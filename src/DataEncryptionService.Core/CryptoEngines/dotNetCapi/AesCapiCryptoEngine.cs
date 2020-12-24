using System;
using System.Linq;
using System.Security.Cryptography;
using DataEncryptionService.Configuration;
using DataEncryptionService.CryptoEngines;

namespace DataEncryptionService.Core.CryptoEngines.dotNetCapi
{
    public class AesCapiCryptoEngine : BaseLegacyCryptoEngine, ICryptographicEngine
    {
        // NOTE: More details on .NET cryptographic services are available here:
        // https://docs.microsoft.com/en-us/dotnet/standard/security/cross-platform-cryptography

        private readonly bool _isConfigured = false;

        public AesCapiCryptoEngine(DataEncryptionServiceConfiguration config)
        {
            config.Encryption.ActiveKeys.TryGetValue(EngineId.ToString("N"), out string activeKey);
            ServiceConfigEncryptionKeyConfiguration keyConfig = config.Encryption.KeyConfigurations
                                                                                    .Where(x => x.Name == activeKey)
                                                                                    .FirstOrDefault();
            if (null != keyConfig && !string.IsNullOrEmpty(keyConfig.Key) && !string.IsNullOrEmpty(keyConfig.IV))
            {
                var cryptoProvider = new AesCryptoServiceProvider();
                InitializeProvider(cryptoProvider, keyConfig.Key, keyConfig.IV, activeKey);
                _isConfigured = true;
            }
        }

        public string DisplayName => WellKnownConstants.DotNet.AesCapi.Name;
        public Guid EngineId => WellKnownConstants.DotNet.AesCapi.CryptoEngineUUID;
        public bool IsConfigured => _isConfigured;
    }
}