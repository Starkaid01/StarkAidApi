namespace StarkAid.Api.Controllers
{
    internal class TokenResult
    {
        public dynamic AccessToken { get; set; }
        public dynamic RefreshToken { get; set; }
        public dynamic Uid { get; set; }
        public dynamic ExpireTime { get; set; }
    }
}