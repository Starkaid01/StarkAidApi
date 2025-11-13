namespace StarkAid.Api.DTOs.WPPconnect
{
    public class SendMessageDto
    {
        public string UserId { get; set; }
        public string SessionName { get; set; }
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
        public bool IsGroup { get; set; } = false;
        public bool IsNewsletter { get; set; } = false;
        public bool IsLid { get; set; } = false;

        public string Estilo { get; set; } = string.Empty;
    }
}
