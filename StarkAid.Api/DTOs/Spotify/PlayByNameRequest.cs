namespace StarkAid.Api.DTOs.Spotify
{
    public class PlayByNameRequest
    {
        public Guid UserId { get; set; }
        public string TrackName { get; set; }
    }

}
