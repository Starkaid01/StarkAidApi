namespace StarkAid.Web.DTOs
{
    public class EwelinkAccountDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public long AccessTokenExpiry { get; set; }
        public long RefreshTokenExpiry { get; set; }
        public string Region { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
