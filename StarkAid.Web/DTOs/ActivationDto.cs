namespace StarkAid.Web.DTOs
{
    public class ActivationDto
    {
        public string Id { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public DateTime ActivatedAt { get; set; }
        public DateTime? DeactivatedAt { get; set; }
        public bool IsActive { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }
}
