using System;

namespace DataEncryptionService.CryptoEngines
{
    public interface ICryptoEngineFactory
    {
        ICryptographicEngine GetDefaultEngine();
        ICryptographicEngine GetEngine(Guid engineId);
    }
}
