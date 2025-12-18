namespace StarkAid.Web.Dtos
{
    public class UserEconomyDto
    {
        public string PlanType { get; set; } = string.Empty;
        public int StarkCoinBalance { get; set; }

        public int TokensConsumidosSemana { get; set; }
        public int TokensSemanaMax { get; set; }
        public int TokensRestantes { get; set; }

        public bool AdsEnabled { get; set; }

        public int AgendamentosMax { get; set; }
        public int AgendamentosRestantes { get; set; }

        public int Rate { get; set; }
    }
}
