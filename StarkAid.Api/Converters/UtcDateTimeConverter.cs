using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarkAid.Api.Converters
{
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateTimeOffset = reader.GetDateTimeOffset();
            return dateTimeOffset.UtcDateTime;
        }


        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime());
        }
    }
}