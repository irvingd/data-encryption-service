using System;
using System.Linq;
using System.Security.Cryptography;
using DataEncryptionService.Configuration;
using DataEncryptionService.CryptoEngines;

namespace DataEncryptionService.Core.CryptoEngines.dotNetCapi
{
    public class TripleDesCapiCryptoEngine : BaseLegacyCryptoEngine, ICryptographicEngine
    {
        // NOTE: More details on .NET cryptographic services are available here:
        // https://docs.microsoft.com/en-us/dotnet/standard/security/cross-platform-cryptography

        private readonly bool _isConfigured = false;

        public TripleDesCapiCryptoEngine(DataEncryptionServiceConfiguration config)
        {
            // NOTE: This implementation is only here to validate the "plug-in" seoftware architecture and to
            //       satisfacy specific designs that must support some legacy encrypted value.
            //       3DES cryptography is not recommended in modern designs

            config.Encryption.ActiveKeys.TryGetValue(EngineId.ToString("N"), out string activeKey);
            ServiceConfigEncryptionKeyConfiguration keyConfig = config.Encryption.KeyConfigurations
                                                                                    .Where(x => x.Name == activeKey)
                                                                                    .FirstOrDefault();
            if (null != keyConfig && !string.IsNullOrEmpty(keyConfig.Key) && !string.IsNullOrEmpty(keyConfig.IV))
            {
                using (var cryptoProvider = new TripleDESCryptoServiceProvider())
                {
                    InitializeProvider(cryptoProvider, keyConfig.Key, keyConfig.IV, activeKey);
                    _isConfigured = true;
                }
            }
        }

        public string DisplayName => WellKnownConstants.DotNet.TripleDesCapi.Name;
        public Guid EngineId => WellKnownConstants.DotNet.TripleDesCapi.CryptoEngineUUID;
        public bool IsConfigured => _isConfigured;
    }
}