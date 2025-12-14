namespace StarkAid.Api.DTOs.WPPconnect
{
    public class SendMessageDto
    {
public string UserId { get; set; } = string.Empty;
public string SessionName { get; set; } = string.Empty;
public string PhoneNumber { get; set; } = string.Empty;
public string Message { get; set; } = string.Empty;
        public bool IsGroup { get; set; } = false;
        public bool IsNewsletter { get; set; } = false;
        public bool IsLid { get; set; } = false;

        public string Estilo { get; set; } = string.Empty;
    }
}
