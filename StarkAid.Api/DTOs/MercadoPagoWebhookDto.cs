namespace StarkAid.Api.DTOs;

public class MercadoPagoWebhookDto
{
    public string Action { get; set; }
    public string Type { get; set; }
    public string DataId { get; set; }
}
