using System;

namespace StarkAid.Api.Entities
{
    public enum MusicKind
    {
        Song = 0,
        Artist = 1,
        Genre = 2 // For future use
    }

    public class YouTubeMusicCache
    {
        public int Id { get; set; }
        public string NormalizedQuery { get; set; } = string.Empty;
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Channel { get; set; }
        public int DurationSeconds { get; set; }
        public bool IsLive { get; set; }
        public string Source { get; set; } = "YouTube";
        public MusicKind Kind { get; set; } = MusicKind.Song;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastUsedAt { get; set; } = DateTimeOffset.UtcNow;
        public int HitCount { get; set; } = 1;
    }
}
