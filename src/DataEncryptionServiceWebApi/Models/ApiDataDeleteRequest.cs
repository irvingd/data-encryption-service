using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataEncryptionService.WebApi.Models
{
    public class ApiDataDeleteRequest
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; }
    }
}
