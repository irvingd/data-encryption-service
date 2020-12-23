using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataEncryptionService.WebApi.Models
{
    public class DecryptedData
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("data")]
        public Dictionary<string, string> Data { get; set; }

        [JsonPropertyName("error_code")]
        public int? ErrorCode { get; set; }

        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; }
    }
}
