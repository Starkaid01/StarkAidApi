namespace StarkAid.Api.DTOs.V1.Spotify
{
    public class PlayByNameRequest
    {
        public Guid UserId { get; set; }
public string TrackName { get; set; } = string.Empty;
    }

}
