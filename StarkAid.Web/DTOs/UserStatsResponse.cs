namespace StarkAid.Web.DTOs
{
    public class UserStatsResponse
    {
        public int TotalDevices { get; set; }
        public int TotalCommands { get; set; }
        public int StarkCoins { get; set; }
        public string CurrentPlan { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public int TokensRemaining { get; set; }
    }
}
