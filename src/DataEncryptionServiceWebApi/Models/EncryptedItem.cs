using System.Text.Json.Serialization;

namespace DataEncryptionService.WebApi.Models
{
    public class EncryptedItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("hash")]
        public string Hash { get; set; }
    }
}
