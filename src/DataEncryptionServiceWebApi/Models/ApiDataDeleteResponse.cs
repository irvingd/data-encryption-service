using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataEncryptionService.WebApi.Models
{
    public class ApiDataDeleteResponse : ApiBaseResponse
    {
        [JsonPropertyName("label_responses")]
        public List<DeleteLabel> LabelResponses { get; set; }

        [JsonPropertyName("has_errors")]
        public bool HasErrors { get; set; }
    }
}
