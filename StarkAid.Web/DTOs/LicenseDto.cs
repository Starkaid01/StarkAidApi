namespace StarkAid.Web.DTOs
{
    public class LicenseDto
    {
        public string Id { get; set; } = string.Empty;
        public string LicenseKey { get; set; } = string.Empty;
        public int MaxMachines { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime? PaymentConfirmedAt { get; set; }
        public int ActiveActivations { get; set; }
        public List<ActivationDto> Activations { get; set; } = new();
    }
}
