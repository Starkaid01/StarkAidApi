using StarkAid.Web.Dtos;

namespace StarkAid.Web.DTOs
{
    public class ComandosSociaisResponseDto
    {
        public List<ComandoSocialDto> Comandos { get; set; } = new();
        public UserEconomyDto Economy { get; set; } = new();
    }
}
