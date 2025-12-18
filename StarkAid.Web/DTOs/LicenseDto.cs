namespace StarkAid.Web.DTOs
{
    public class LicenseDto
    {
        public string LicenseKey { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public bool IsActive { get; set; }
    }
}
