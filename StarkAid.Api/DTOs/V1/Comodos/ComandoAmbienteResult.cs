namespace StarkAid.Api.DTOs.V1.Comodos
{
    public class ComandoAmbienteResult
    {
        public bool Sucesso { get; set; }
        public string MensagemVoz { get; set; } = string.Empty;
        public bool RequerConfirmacao { get; set; }
        public List<Guid>? DispositivosAcionados { get; set; }
    }
}
