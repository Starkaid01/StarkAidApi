using System;

namespace StarkAid.Api.DTOs.V1.Music
{
    public class ExternalAudioStreamResult
    {
        public string StreamUrl { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
