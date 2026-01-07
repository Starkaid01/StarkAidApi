namespace StarkAid.Api.DTOs.V1.Comodos
{
    public class AssociateDeviceRequest
    {
        public string DispositivoId { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string? Papel { get; set; }
    }
}
