namespace StarkAid.WindowsForms.Models;

public class Lembrete
{
    public int Id { get; set; }
    public string Lembrar { get; set; } = string.Empty; // Ação que usuário pediu
    public int? Dia { get; set; } // Dia do mês (1-31)
    public int? Mes { get; set; } // Mês (1-12)
    public int? Hora { get; set; } // Hora (0-23)
    public int? Minuto { get; set; } // Minuto (0-59)
    public bool Concluido { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? UltimaNotificacao { get; set; } // Para controlar repetição a cada 2 minutos
}
