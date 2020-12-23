using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataEncryptionService.WebApi.Models
{
    public class ApiDataDecryptRequest
    {
        [JsonPropertyName("labeled_items")]
        public List<LabeledItemSet> Items { get; set; }
    }
}
