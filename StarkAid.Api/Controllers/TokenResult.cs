namespace StarkAid.Api.Controllers
{
    internal class TokenResult
    {
        public object AccessToken { get; set; } = default!;
        public object RefreshToken { get; set; } = default!;
        public object Uid { get; set; } = default!;
        public object ExpireTime { get; set; } = default!;
    }
}