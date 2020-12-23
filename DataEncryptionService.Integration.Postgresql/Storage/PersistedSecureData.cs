using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper.Contrib.Extensions;
using DataEncryptionService.Storage;

namespace DataEncryptionService.Integration.Postgresql.Storage
{
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Note: This implementation needs to be refactored. Need to have a better mechanism to map PostgreSQL column names.
    ///       Consider Dapper or direct Npgsql calls.
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [Table("encrypted_data")]
    public class PersistedSecureData : IPersistedSecureData
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        [Key]
        public long Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime EncryptedOn { get; set; }
        public string Label { get; set; }
        public Guid EngineId { get; set; }
        public string KeyScope { get; set; }
        public string KeyName { get; set; }
        public int KeyVersion { get; set; }
        public string HashMethod { get; set; }
        public string EngineRequestId { get; set; }
        public List<EncryptedDataItem> Data { get; set; }
        public Dictionary<string, object> EncryptionParameters { get; set; }
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

        public string[] TagsArray
        {
            get
            {
                return Tags?.ToArray();
            }
            set
            {
                if (value?.Length > 0)
                {
                    Tags = new HashSet<string>(value);
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

    [Table("encrypted_data")]
    internal class PgSecureData
    {
        [Key]
        public long row_id { get; set; }
        public DateTime created_on { get; set; }
        public string data_label { get; set; }
        public DateTime encrypted_on { get; set; }
        public Guid engine_id { get; set; }
        public string key_scope { get; set; }
        public string key_name { get; set; }
        public int key_version { get; set; }
        public string hash_method { get; set; }
        public string engine_request_id { get; set; }
        public string json_data { get; set; }
        public string json_enc_params { get; set; }
        public string[] tags { get; set; }
    }
}
