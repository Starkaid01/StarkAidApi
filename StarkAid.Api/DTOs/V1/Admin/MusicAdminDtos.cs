using StarkAid.Api.Entities;

namespace StarkAid.Api.DTOs.V1.Admin
{
    public class AdminUpdateSystemConfigRequest
    {
        public string? DominioAudioResolver { get; set; }
        public string? DominioCloudflare { get; set; }
        public string? DominioNlp { get; set; }
    }

    public class YouTubeMusicCacheDto
    {
        public int Id { get; set; }
        public string NormalizedQuery { get; set; } = string.Empty;
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Channel { get; set; }
        public int Kind { get; set; }
        public int HitCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastUsedAt { get; set; }
    }

    public class AdminMusicCacheRequest
    {
        public string NormalizedQuery { get; set; } = string.Empty;
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Channel { get; set; }
        public MusicKind Kind { get; set; } = MusicKind.Song;
    }

    public class MergeSuggestionDto
    {
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public double Score { get; set; }
        public bool IsBlocked { get; set; }
        public string Reason { get; set; } = string.Empty; // e.g. "Similaridade alta", "Blocked by seed"
    }

    public class MergeExecutionRequest
    {
        public string SourceQuery { get; set; } = string.Empty;
        public string TargetCanonical { get; set; } = string.Empty;
    }
}
