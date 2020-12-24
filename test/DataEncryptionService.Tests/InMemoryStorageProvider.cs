using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataEncryptionService.Integration.MongoDB.Storage;
using DataEncryptionService.Storage;
using MongoDB.Bson;

namespace DataEncryptionService.Tests
{
    public class InMemoryStorageProvider : IStorageProvider
    {
        public static Guid UUID = Guid.Parse("FF000000-0000-0000-0000-000000000001");
        public string DisplayName => "In-Memory Only Storage Provider (for Unit Testing)";
        public Guid ProviderId => UUID;
        public bool IsConfigured => true;

        public Dictionary<string, PersistedSecureData> _persistedData = new Dictionary<string, PersistedSecureData>();

        public IPersistedSecureData AllocateNewData() => PersistedSecureData.CreateDefault();


        public Task<IPersistedSecureData> LoadEncryptedDataAsync(string tag)
        {
            _persistedData.TryGetValue(tag, out PersistedSecureData dataDoc);
            return Task.FromResult((IPersistedSecureData)dataDoc);
        }

        public Task SaveEncryptedDataAsync(IPersistedSecureData data)
        {
            PersistedSecureData dataDocument = data as PersistedSecureData;
            if (dataDocument.Id == ObjectId.Empty)
            {
                dataDocument.Id = ObjectId.GenerateNewId();
                dataDocument.CreatedOn = dataDocument.EncryptedOn = DateTime.UtcNow;
                _persistedData.TryAdd(dataDocument.Label, dataDocument);
            }
            else
            {
                string existingLabel = _persistedData.Values.FirstOrDefault(x => x.Id == dataDocument.Id)?.Label;
                if (!string.IsNullOrEmpty(existingLabel))
                {
                    _persistedData.Remove(existingLabel);
                    dataDocument.EncryptedOn = DateTime.UtcNow;
                    _persistedData.TryAdd(dataDocument.Label, dataDocument);
                }
            }

            return Task.CompletedTask;
        }

        public Task<bool> DeleteEncryptedDataAsync(string tag)
        {
            bool removed = _persistedData.Remove(tag);
            return Task.FromResult(removed);
        }

        public Task<IEnumerable<IPersistedSecureData>> GetEnumerableListAsync(string lastProcessedLabel = null, Guid? cryptoEngineId = null, string keyName = null, string keyScope = null, int? keyVersion = null, DateTime? fromEncryptedOn = null)
        {
            throw new NotImplementedException();
        }
    }
}