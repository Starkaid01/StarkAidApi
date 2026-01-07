using StarkAid.Api.DTOs;

namespace StarkAid.Api.DTOs.V1.Music
{
    public class MusicResolveRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class MusicResolveResponse
    {
        public string Type { get; set; } = string.Empty; // "radio", "youtube", "stop", "pause", "resume", "next", "volume_up", "volume_down", "status", "error"
        public string Source { get; set; } = string.Empty; // "radio", "youtube"
        public string Tts { get; set; } = string.Empty;
        public MusicStationStation? Station { get; set; }
        public string? YouTubeVideoId { get; set; }
        public string? Title { get; set; }
        public double Confidence { get; set; }
        public EconomicPayload? Economy { get; set; }
    }

    public class MusicStationStation
    {
        public string Name { get; set; } = string.Empty;
        public string StreamUrl { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Bitrate { get; set; }
    }
}
