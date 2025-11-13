using Amazon.Util;

namespace StarkAid.Api.DTOs.WPPconnect
{
    public class CreateSessionDto
    {
        public string UserId { get; set; } = null!;
        public string SessionName { get; set; } = null!;
        // Adicione isso com valor default vazio
        public string Webhook { get; set; } = "";
        public bool WaitQrCode { get; set; }
        public string Proxy { get; set; } = ""!;

    }

}
