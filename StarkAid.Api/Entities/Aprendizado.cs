using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities
{
    public class Aprendizado
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Texto { get; set; } = string.Empty;

        [Required]
        public string Resposta { get; set; } = string.Empty;

        /// <summary>
        /// Contexto de ancoragem (ex: "uso de agua sanitaria em piso de madeira").
        /// Se nulo, é um aprendizado global/direto.
        /// </summary>
        public string? Contexto { get; set; }

        public Guid? UserId { get; set; }

        /// <summary>
        /// "Global", "Usuario" ou "Contextual"
        /// </summary>
        public string Tipo { get; set; } = "Global";

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Quantidade de vezes que este aprendizado foi reutilizado com sucesso.
        /// </summary>
        public int HitCount { get; set; } = 0;

        /// <summary>
        /// Data da última vez que foi utilizado.
        /// </summary>
        public DateTimeOffset? LastUsedAt { get; set; }

        /// <summary>
        /// Pontuação de confiança (1-100). Incrementa com uso, decrementa com feedback negativo.
        /// </summary>
        public int ConfidenceScore { get; set; } = 1;

        /// <summary>
        /// Indica se o aprendizado está ativo e disponível para uso.
        /// </summary>
        public bool Ativo { get; set; } = true;

        /// <summary>
        /// Indica se o item está em período de avaliação antes da inativação (Quarentena).
        /// Se houver uso (Hit) durante este período, ele volta a ser saudável.
        /// </summary>
        public bool EmQuarentena { get; set; } = false;

        public DateTimeOffset? QuarentenaDesde { get; set; }

        /// <summary>
        /// Contador de quantas formas diferentes de perguntar resultaram em um match para este item.
        /// Usado para garantir que o aprendizado generaliza bem antes da promoção para Global.
        /// </summary>
        public int VariantesDistintasUsadas { get; set; } = 0;

        public DateTimeOffset? UltimaRessurreicaoAt { get; set; }

        public ICollection<AprendizadoResposta> Respostas { get; set; } = new List<AprendizadoResposta>();
    }

    public class GcExecutionLog
    {
        [Key]
        public Guid Id { get; set; }
        public DateTimeOffset DataExecucao { get; set; } = DateTimeOffset.UtcNow;
        public int ItensInativados { get; set; }
        public int ItensEmQuarentena { get; set; }
        public int ItensRessuscitados { get; set; }
        public long DuracaoMs { get; set; }
        public string LogDetalhado { get; set; } = string.Empty;
    }

    /// <summary>
    /// Armazena o estado atual do contexto de conversa por usuário para o sistema de Aprendizado IA
    /// </summary>
    public class UserConversaContext
    {
        [Key]
        public Guid UserId { get; set; }
        
        public string ContextoAtual { get; set; } = string.Empty;
        
        public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
