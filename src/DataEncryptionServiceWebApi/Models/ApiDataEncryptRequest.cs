using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataEncryptionService.WebApi.Models
{
    public class ApiDataEncryptRequest
    {
        [JsonPropertyName("data")]
        public Dictionary<string, string> Data { get; set; }
    }
}
