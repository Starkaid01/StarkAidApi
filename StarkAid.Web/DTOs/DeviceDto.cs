using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarkAid.Web.DTOs
{
    public class DeviceDto
    {
        [JsonConverter(typeof(NumberToStringConverter))]
        public string Id { get; set; } = string.Empty;

        public string DeviceId { get; set; } = string.Empty;  // ← ESSA LINHA É OBRIGATÓRIA!

        public string Name { get; set; } = string.Empty;

        [JsonConverter(typeof(NumberToStringConverter))]
        public string Type { get; set; } = string.Empty;

        public bool Online { get; set; }
        public bool IsOn { get; set; }

        [JsonConverter(typeof(NumberToStringConverter))]
        public string FamilyId { get; set; } = string.Empty;

        [JsonConverter(typeof(NumberToStringConverter))]
        public string RoomId { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string MqttTopic { get; set; } = string.Empty;
        public string Comando { get; set; } = string.Empty;
    }

    // Converter personalizado (melhorado para lidar com int e long)
    public class NumberToStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.TryGetInt64(out long l) ? l.ToString() : reader.GetDouble().ToString(),
                JsonTokenType.String => reader.GetString(),
                _ => reader.GetString()
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}