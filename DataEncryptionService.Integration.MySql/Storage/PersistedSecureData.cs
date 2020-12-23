using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using DataEncryptionService.Storage;

namespace DataEncryptionService.Integration.MySql.Storage
{
    [Table("EncryptedData")]
    public class PersistedSecureData : IPersistedSecureData
    {
        [Key]
        public long Id { get; set; }
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
