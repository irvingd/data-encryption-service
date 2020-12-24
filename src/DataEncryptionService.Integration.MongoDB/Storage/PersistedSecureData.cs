using System;
using System.Collections.Generic;
using DataEncryptionService.Storage;
using MongoDB.Bson;

namespace DataEncryptionService.Integration.MongoDB.Storage
{
    public class PersistedSecureData : IPersistedSecureData
    {
        public ObjectId Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Label { get; set; }
        public HashSet<string> Tags { get; set; }
        public DateTime EncryptedOn { get; set; }
        public Guid EngineId { get; set; }
        public string KeyScope { get; set; }
        public string KeyName { get; set; }
        public int KeyVersion { get; set; }
        public string HashMethod { get; set; }
        public string EngineRequestId { get; set; }
        public Dictionary<string, object> EncryptionParameters { get; set; }
        public List<EncryptedDataItem> Data { get; set; }

        static public PersistedSecureData CreateDefault()
        {
            var instance = new PersistedSecureData()
            {
                Label = Guid.NewGuid().ToString("N"),
                Data = new List<EncryptedDataItem>()
            };

            return instance;
        }
    }
}
