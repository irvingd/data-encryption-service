using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper.Contrib.Extensions;
using DataEncryptionService.Storage;

namespace DataEncryptionService.Integration.MSSQL.Storage
{
    [Table("EncryptedData")]
    public class PersistedSecureData : IPersistedSecureData
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        [Key]
        public long Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Label { get; set; }
        public DateTime EncryptedOn { get; set; }
        public Guid EngineId { get; set; }
        public string KeyScope { get; set; }
        public string KeyName { get; set; }
        public int KeyVersion { get; set; }
        public string HashMethod { get; set; }
        public string EngineRequestId { get; set; }

        [Write(false)]
        public List<EncryptedDataItem> Data { get; set; }

        [Write(false)]
        public Dictionary<string, object> EncryptionParameters { get; set; }
        
        [Write(false)]
        public HashSet<string> Tags { get; set; }

        public string JsonData
        {
            get
            {
                return (Data?.Count > 0) ? JsonSerializer.Serialize(Data, jsonOptions) : null;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    Data = null;
                }
                else
                {
                    Data = JsonSerializer.Deserialize<List<EncryptedDataItem>>(value);
                }
            }
        }
        public string JsonEncryptionParams
        {
            get
            {
                return (EncryptionParameters?.Count > 0) ? JsonSerializer.Serialize(EncryptionParameters, jsonOptions) : null;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    EncryptionParameters = null;
                }
                else
                {
                    EncryptionParameters = JsonSerializer.Deserialize<Dictionary<string, object>>(value);
                }
            }
        }
        public string AllTags
        {
            get
            {
                return (Tags?.Count > 0) ? string.Join("|", Tags) : null;
            }
            set
            {
                if (value?.Length > 0)
                {
                    Tags = new HashSet<string>(value.Split('|'));
                }
                else
                {
                    Tags = null;
                }
            }
        }

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