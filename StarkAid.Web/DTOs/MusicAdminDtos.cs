using System.Text.Json.Serialization;

namespace StarkAid.Web.DTOs
{
    public class SystemConfigDto
    {
        public int Id { get; set; }
        public string DominioCloudflare { get; set; } = string.Empty;
        public string DominioNlp { get; set; } = string.Empty;
        public string DominioAudioResolver { get; set; } = string.Empty;
        public DateTime UltimaAtualizacao { get; set; }
    }

    public class YouTubeMusicCacheDto
    {
        public int Id { get; set; }
        public string NormalizedQuery { get; set; } = string.Empty;
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Channel { get; set; }
        public int Kind { get; set; } // 0 = Song, 1 = Artist
        public int HitCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastUsedAt { get; set; }
    }
    public class MergeSuggestionDto
    {
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public double Score { get; set; }
        public bool IsBlocked { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class MergeExecutionRequest
    {
        public string SourceQuery { get; set; } = string.Empty;
        public string TargetCanonical { get; set; } = string.Empty;
    }
}
