using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataEncryptionService.CryptoEngines
{
    public interface ICryptographicEngine
    {
        string DisplayName { get; }
        Guid EngineId { get; }
        bool IsConfigured { get; }

        Task<EncryptionResult> EncryptAsync(IReadOnlyDictionary<string, string> kvClearText, Dictionary<string, object> parameters = null);
        Task<DecryptionResult> DecryptAsync(IReadOnlyDictionary<string, string> kvCipherText, Dictionary<string, object> parameters = null);
        Task<RotateKeyResult> RotateEncryptionKeyAsync(string keyName, Dictionary<string, object> parameters = null);
        Task<EncryptionResult> ReencryptAsync(IReadOnlyDictionary<string, string> kvCipherText, Dictionary<string, object> parameters = null);
        Task<EncryptionKeyVersionResult> GetEncryptionKeyVersionInfoAsync(string keyName, string keyScope, Dictionary<string, object> parameters = null);
    }
}