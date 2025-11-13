using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities
{
    public class LogUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string WhatsappLog { get; set; } = string.Empty;

        public string FullDuplexAssistant { get; set; } = string.Empty;

        public string MainActivityLog { get; set; } = string.Empty;

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
