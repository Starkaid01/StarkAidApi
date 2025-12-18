using StarkAid.Web.Dtos;

namespace StarkAid.Web.DTOs
{
    public class ComandoSocialResponseDto
    {
        public ComandoSocialDto Comando { get; set; } = new();
        public UserEconomyDto Economy { get; set; } = new();
    }
}
