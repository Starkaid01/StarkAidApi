using System.Text.Json.Serialization;

namespace StarkAid.Api.DTOs.V1.Nlp
{
    public class ExtractEntitiesRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
