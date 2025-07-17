namespace StarkAid.Api.Entities;

public class ComandoSocial
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Comando { get; set; }
    public string Resposta { get; set; }
}