using System;
using System.Collections.Generic;
using StarkAid.Api.Entities;

namespace StarkAid.Api.DTOs.Rotinas
{
    public class RotinaDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public bool Ativa { get; set; }
        public DateTimeOffset CriadaEm { get; set; }
        public DateTimeOffset AtualizadaEm { get; set; }

        public List<RotinaGatilhoDto> Gatilhos { get; set; } = new();
        public List<RotinaAcaoDto> Acoes { get; set; } = new();
    }

    public class RotinaGatilhoDto
    {
        public Guid Id { get; set; }
        public TipoGatilho Tipo { get; set; }
        public string Expressao { get; set; } = string.Empty;
        public string? DiasSemana { get; set; }
    }

    public class RotinaAcaoDto
    {
        public Guid Id { get; set; }
        public int OrdemExecucao { get; set; }
        public TipoAcao Tipo { get; set; }
        public string Payload { get; set; } = string.Empty;
    }

    public class CreateRotinaRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public List<CreateRotinaGatilhoRequest> Gatilhos { get; set; } = new();
        public List<CreateRotinaAcaoRequest> Acoes { get; set; } = new();
    }

    public class CreateRotinaGatilhoRequest
    {
        public TipoGatilho Tipo { get; set; }
        public string Expressao { get; set; } = string.Empty;
        public string? DiasSemana { get; set; }
    }

    public class CreateRotinaAcaoRequest
    {
        public int OrdemExecucao { get; set; }
        public TipoAcao Tipo { get; set; }
        public string Payload { get; set; } = string.Empty;
    }

    public class UpdateRotinaRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public bool Ativa { get; set; }
        public List<CreateRotinaGatilhoRequest> Gatilhos { get; set; } = new();
        public List<CreateRotinaAcaoRequest> Acoes { get; set; } = new();
    }
}
