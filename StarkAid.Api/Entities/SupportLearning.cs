using System;
using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class SupportLearning
{
    [Key]
    public int Id { get; set; }
    
    public Guid? UserId { get; set; } // nullable para global
    
    [Required]
    public string UserEntradaTxt { get; set; } = string.Empty;
    
    [Required]
    public string IAResponseTxt { get; set; } = string.Empty;
    
    [Required]
    public string ContextTitle { get; set; } = string.Empty;
    
    public int ConfidenceScore { get; set; } // 0–100
    
    public int UsageCount { get; set; }
    
    public bool IsGlobal { get; set; }
    
    public bool IsQuarantined { get; set; }
    
    public bool IsDisabled { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastUsedAt { get; set; }
}
