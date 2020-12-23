using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataEncryptionService.WebApi.Models
{
    public class ApiDataDecryptResponse : ApiBaseResponse
    {
        [JsonPropertyName("data")]
        public List<DecryptedData> LabeledData { get; set; }

        [JsonPropertyName("has_errors")]
        public bool HasErrors { get; set; }
    }
}
