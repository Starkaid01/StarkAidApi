namespace StarkAid.Api.Entities
{
    public enum TipoGatilho
    {
        Tempo,      // Baseado em horário (ex: 08:00)
        Comando,    // Voz ou texto (ex: "boa noite")
        Evento      // Sensores ou eventos IoT
    }

    public enum TipoAcao
    {
        Dispositivo, // Ligar/Desligar
        Comando,     // Executar outro comando (ex: "previsão do tempo")
        Delay,       // Aguardar X segundos
        Notificacao, // Enviar notificação push para o app
        AbrirUrl,     // Abrir um link no aplicativo
        ComandoAssistente // Enviar comando para o App processar como voz
    }
}
