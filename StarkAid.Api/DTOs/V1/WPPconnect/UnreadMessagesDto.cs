namespace StarkAid.Api.DTOs.V1.WPPconnect
{
    public class UnreadMessagesDto
    {
        public string UserId { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty; // Usado apenas no tem-mensagem-nao-lida
    }
}
