using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataEncryptionService.WebApi
{
    public class LabeledItemSet
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("items")]
        public HashSet<string> Items { get; set; }
    }
}
