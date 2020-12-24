using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataEncryptionService.Storage
{
    public interface IStorageProvider
    {
        string DisplayName { get; }
        Guid ProviderId { get; }
        bool IsConfigured { get; }

        IPersistedSecureData AllocateNewData();

        Task SaveEncryptedDataAsync(IPersistedSecureData data);

        Task<IPersistedSecureData> LoadEncryptedDataAsync(string label);

        Task<bool> DeleteEncryptedDataAsync(string label);

        Task<IEnumerable<IPersistedSecureData>> GetEnumerableListAsync(
            string lastProcessedLabel = null,
            Guid? cryptoEngineId = null,
            string keyName = null,
            string keyScope = null,
            int? keyVersion = null,
            DateTime? fromEncryptedOn = null);
    }
}
