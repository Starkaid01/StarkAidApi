using System.Text.Json.Serialization;

namespace StarkAid.Api.DTOs.Nlp
{
    public class ExtractEntitiesRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
