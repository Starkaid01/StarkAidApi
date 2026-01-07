using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities
{
    public class MusicArtistAlias
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Alias { get; set; } = string.Empty; // ex: "charlie brown"

        [Required]
        [MaxLength(200)]
        public string Canonical { get; set; } = string.Empty; // ex: "charlie brown jr"
    }
}
