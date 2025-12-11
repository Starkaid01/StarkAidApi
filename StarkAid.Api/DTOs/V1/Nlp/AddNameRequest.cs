using System.Text.Json.Serialization;

namespace StarkAid.Api.DTOs.V1.Nlp
{
    public class AddNameRequest
    {
        [JsonPropertyName("full_name")]
        public string Full_Name { get; set; } = string.Empty;
    }
}
