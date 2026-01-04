namespace StarkAid.Web.DTOs
{
    public class ErrorLogDto
    {
        public Guid Id { get; set; }
        public string Source { get; set; } // "App" or "Soft"
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
