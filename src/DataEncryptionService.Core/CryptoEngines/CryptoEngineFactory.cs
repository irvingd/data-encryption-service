using System;
using System.Collections.Generic;
using System.Linq;
using DataEncryptionService.Configuration;
using DataEncryptionService.CryptoEngines;

namespace DataEncryptionService.Core.CryptoEngines
{
    public class CryptoEngineFactory : ICryptoEngineFactory
    {
        private readonly ServiceConfigEncryption _encryptionConfig;
        private readonly IEnumerable<ICryptographicEngine> _engines;

        public CryptoEngineFactory(DataEncryptionServiceConfiguration config, IEnumerable<ICryptographicEngine> engines)
        {
            _encryptionConfig = config.Encryption;

            // Only add the engines that are fully configured and can be used. Each implementation decides
            // what it requires to consider itself in a "configured" state
            _engines = engines.Where(item => item.IsConfigured);
        }

        public ICryptographicEngine GetDefaultEngine()
        {
            ICryptographicEngine engine = _engines
                                            .Where(s => s.EngineId == _encryptionConfig.DefaultEngine)
                                            .FirstOrDefault();
            // TODO: handle if no default
            return engine;
        }

        public ICryptographicEngine GetEngine(Guid engineId)
        {
            ICryptographicEngine engine = _engines
                                            .Where(s => s.EngineId == engineId)
                                            .FirstOrDefault();
            // TODO: handle if not found
            return engine;
        }
    }
}