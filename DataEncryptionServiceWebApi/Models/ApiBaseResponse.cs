using System.Text.Json.Serialization;

namespace DataEncryptionService.WebApi.Models
{
    public abstract class ApiBaseResponse
    {
        [JsonPropertyName("error_code")]
        public int? ErrorCode { get; set; }

        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("service_request_id")]
        public string RequestId { get; set; }
    }
}
