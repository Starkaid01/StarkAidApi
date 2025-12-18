namespace StarkAid.Web.DTOs
{
    public class EwelinkStatusResponse
    {
        public bool IsLoggedIn { get; set; }
        public EwelinkAccountDto Account { get; set; } = new();
    }
}
