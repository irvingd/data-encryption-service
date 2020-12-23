using System.Text.Json.Serialization;

namespace DataEncryptionService.WebApi.Models
{
    public class DeleteLabel
    {
        [JsonPropertyName("error_code")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }
    }
}
