using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Data;

public static class SeedErrorCodes
{
    public static void SeedErrorCodeDescriptions(AppDbContext context)
    {
        // Buscar todos os códigos existentes (com tracking para permitir atualização)
        var existingCodes = context.ErrorCodeDescriptions
            .ToDictionary(e => e.CodigoDeErro, e => e);
        
        var errorCodesToAdd = new List<ErrorCodeDescription>();
        bool hasUpdates = false;
        
        // Adicionar códigos de app se não existirem ou atualizar se não tiverem soluções
        var appCodes = GetAppErrorCodes();
        foreach (var code in appCodes)
        {
            if (existingCodes.TryGetValue(code.CodigoDeErro, out var existing))
            {
                // Se existe mas não tem soluções, atualizar
                if (string.IsNullOrEmpty(existing.Solucoes) && !string.IsNullOrEmpty(code.Solucoes))
                {
                    existing.Solucoes = code.Solucoes;
                    hasUpdates = true;
                }
            }
            else
            {
                errorCodesToAdd.Add(code);
            }
        }
        
        // Adicionar códigos de soft se não existirem ou atualizar se não tiverem soluções
        var softCodes = GetSoftErrorCodes();
        foreach (var code in softCodes)
        {
            if (existingCodes.TryGetValue(code.CodigoDeErro, out var existing))
            {
                // Se existe mas não tem soluções, atualizar
                if (string.IsNullOrEmpty(existing.Solucoes) && !string.IsNullOrEmpty(code.Solucoes))
                {
                    existing.Solucoes = code.Solucoes;
                    hasUpdates = true;
                }
            }
            else
            {
                errorCodesToAdd.Add(code);
            }
        }
        
        // Adicionar novos códigos
        if (errorCodesToAdd.Count > 0)
        {
            context.ErrorCodeDescriptions.AddRange(errorCodesToAdd);
        }
        
        // Salvar todas as mudanças
        if (errorCodesToAdd.Count > 0 || hasUpdates)
        {
            context.SaveChanges();
        }
    }
    
    private static List<ErrorCodeDescription> GetAppErrorCodes()
    {
        return new List<ErrorCodeDescription>
        {
            // Erros de IA
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_001",
                Descricao = "Erro ao processar comando de IA",
                Contexto = "chamarIaSuper",
                CamposRelevantes = "ultimoComando, ultimaResposta",
                Origem = "app",
                Solucoes = System.Text.Json.JsonSerializer.Serialize(new List<string>
                {
                    "Verificar se o comando enviado está no formato correto",
                    "Verificar a conexão com o serviço de IA",
                    "Verificar se há tokens disponíveis para uso da IA",
                    "Revisar o campo 'ultimoComando' para identificar o problema",
                    "Verificar logs de erro completos para mais detalhes"
                })
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_002",
                Descricao = "Erro ao chamar API de IA",
                Contexto = "chamarIaSuper",
                CamposRelevantes = "ultimoComando",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_003",
                Descricao = "Erro ao processar resposta da IA",
                Contexto = "chamarIaSuper",
                CamposRelevantes = "ultimaResposta",
                Origem = "app"
            },
            
            // Erros de Rede
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_101",
                Descricao = "Erro de conexão de rede",
                Contexto = "requisicoes HTTP",
                CamposRelevantes = "erroCompleto",
                Origem = "app",
                Solucoes = System.Text.Json.JsonSerializer.Serialize(new List<string>
                {
                    "Verificar conexão com a internet",
                    "Verificar se o servidor está acessível",
                    "Verificar configurações de firewall ou proxy",
                    "Tentar novamente após alguns segundos",
                    "Verificar se o dispositivo está conectado à rede Wi-Fi ou dados móveis"
                })
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_102",
                Descricao = "Timeout de requisição",
                Contexto = "requisicoes HTTP",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_103",
                Descricao = "Erro HTTP não tratado",
                Contexto = "requisicoes HTTP",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_104",
                Descricao = "Erro ao fazer requisição API",
                Contexto = "requisicoes HTTP",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            
            // Erros de Dispositivos IoT
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_201",
                Descricao = "Erro ao carregar dispositivos eWeLink",
                Contexto = "carregarDispositivosEwelink",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_202",
                Descricao = "Erro ao controlar dispositivo eWeLink",
                Contexto = "controlarDispositivoEwelink",
                CamposRelevantes = "ultimoComando, ultimoDispositivoAcionado",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_203",
                Descricao = "Erro ao obter status do dispositivo",
                Contexto = "obterStatusDispositivo",
                CamposRelevantes = "ultimoDispositivoAcionado",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_204",
                Descricao = "Erro ao conectar com dispositivo ESP",
                Contexto = "conectarDispositivoESP",
                CamposRelevantes = "ultimoDispositivoAcionado",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_205",
                Descricao = "Erro ao acionar dispositivo ESP",
                Contexto = "acionarDispositivoESP",
                CamposRelevantes = "ultimoComando, ultimoDispositivoAcionado",
                Origem = "app"
            },
            
            // Erros de Banco de Dados Local
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_301",
                Descricao = "Erro ao acessar banco de dados local",
                Contexto = "operacoes de banco de dados",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_302",
                Descricao = "Erro ao salvar dados no banco",
                Contexto = "operacoes de banco de dados",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_303",
                Descricao = "Erro ao ler dados do banco",
                Contexto = "operacoes de banco de dados",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_304",
                Descricao = "Erro ao deletar dados do banco",
                Contexto = "operacoes de banco de dados",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            
            // Erros de UI
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_401",
                Descricao = "Erro ao carregar interface",
                Contexto = "carregamento de UI",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_402",
                Descricao = "Erro ao atualizar UI",
                Contexto = "atualizacao de UI",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_403",
                Descricao = "Erro ao navegar entre telas",
                Contexto = "navegacao",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_404",
                Descricao = "Erro ao renderizar componente",
                Contexto = "renderizacao de UI",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            
            // Erros de JSON
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_501",
                Descricao = "Erro ao parsear JSON",
                Contexto = "processamento de JSON",
                CamposRelevantes = "ultimaResposta, erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_502",
                Descricao = "Erro ao serializar JSON",
                Contexto = "processamento de JSON",
                CamposRelevantes = "ultimoComando, erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_503",
                Descricao = "JSON malformado",
                Contexto = "processamento de JSON",
                CamposRelevantes = "ultimaResposta, erroCompleto",
                Origem = "app"
            },
            
            // Erros de Autenticação
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_601",
                Descricao = "Erro ao fazer login",
                Contexto = "autenticacao",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_602",
                Descricao = "Token expirado",
                Contexto = "validacao de token",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_603",
                Descricao = "Erro ao validar token",
                Contexto = "validacao de token",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_604",
                Descricao = "Erro ao fazer logout",
                Contexto = "autenticacao",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            
            // Erros de TTS/STT
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_701",
                Descricao = "Erro no Text-to-Speech",
                Contexto = "sintese de voz",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_702",
                Descricao = "Erro no Speech-to-Text",
                Contexto = "reconhecimento de voz",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_703",
                Descricao = "Erro ao inicializar reconhecimento de voz",
                Contexto = "inicializacao de STT",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_704",
                Descricao = "Erro ao processar áudio",
                Contexto = "processamento de audio",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            
            // Erros de Inicialização
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_801",
                Descricao = "Erro ao inicializar aplicativo",
                Contexto = "onCreate",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_802",
                Descricao = "Erro ao carregar configurações",
                Contexto = "carregamento de configuracao",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_803",
                Descricao = "Erro ao inicializar serviços",
                Contexto = "inicializacao de servicos",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_804",
                Descricao = "Erro ao verificar permissões",
                Contexto = "verificacao de permissoes",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            
            // Erros de WebSocket
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_901",
                Descricao = "Erro ao conectar WebSocket",
                Contexto = "conexao WebSocket",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_902",
                Descricao = "Erro ao enviar mensagem WebSocket",
                Contexto = "envio WebSocket",
                CamposRelevantes = "ultimoComando, erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_903",
                Descricao = "Erro ao receber mensagem WebSocket",
                Contexto = "recepcao WebSocket",
                CamposRelevantes = "ultimaResposta, erroCompleto",
                Origem = "app"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_904",
                Descricao = "Erro ao desconectar WebSocket",
                Contexto = "desconexao WebSocket",
                CamposRelevantes = "erroCompleto",
                Origem = "app"
            },
            
            // Erros Críticos Inesperados
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_999",
                Descricao = "Erro crítico inesperado",
                Contexto = "erro nao categorizado",
                CamposRelevantes = "erroCompleto",
                Origem = "app",
                Solucoes = System.Text.Json.JsonSerializer.Serialize(new List<string>
                {
                    "Reiniciar o aplicativo",
                    "Verificar se há atualizações disponíveis",
                    "Limpar cache e dados do aplicativo",
                    "Reinstalar o aplicativo se o problema persistir",
                    "Contatar o suporte com os detalhes do erro completo"
                })
            }
        };
    }
    
    private static List<ErrorCodeDescription> GetSoftErrorCodes()
    {
        return new List<ErrorCodeDescription>
        {
            // ========== CÓDIGOS DE ERRO PARA SOFT (Windows Forms) ==========
            
            // Erros de Comandos e Processamento
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_001",
                Descricao = "Erro ao processar comando de IA",
                Solucoes = System.Text.Json.JsonSerializer.Serialize(new List<string>
                {
                    "Verificar se o comando está no formato correto",
                    "Verificar conexão com o serviço de IA",
                    "Verificar se há créditos/tokens disponíveis",
                    "Revisar o campo 'ultimoComando' para identificar o problema",
                    "Verificar logs de erro completos para mais detalhes"
                }),
                Contexto = "ProcessIaCommandAsync",
                CamposRelevantes = "ultimoComando, ultimaResposta",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_002",
                Descricao = "Erro ao processar comando de dispositivo ESP",
                Contexto = "ProcessDeviceCommandsAsync",
                CamposRelevantes = "ultimoDispositivoAcionado",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_003",
                Descricao = "Erro ao processar comando de dispositivo Ewelink",
                Contexto = "ProcessEwelinkDeviceCommandsAsync",
                CamposRelevantes = "ultimoDispositivoAcionado",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_004",
                Descricao = "Erro ao processar comando de dispositivo StarkSwitch",
                Contexto = "ProcessStarkSwitchDeviceCommandsAsync",
                CamposRelevantes = "ultimoDispositivoAcionado",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_005",
                Descricao = "Erro ao processar comando de lembrete/alarme",
                Contexto = "ProcessLembreteCommandAsync",
                CamposRelevantes = "ultimoComando, ultimaResposta",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_006",
                Descricao = "Erro ao processar comando social",
                Contexto = "ProcessSocialCommandAsync",
                CamposRelevantes = "ultimoComando, ultimaResposta",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_007",
                Descricao = "Erro ao processar resposta UDP recebida",
                Contexto = "ProcessUdpResponse",
                CamposRelevantes = "ultimaResposta",
                Origem = "soft",
                Solucoes = System.Text.Json.JsonSerializer.Serialize(new List<string>
                {
                    "Verificar se a resposta UDP está no formato esperado",
                    "Verificar se o servidor UDP está respondendo corretamente",
                    "Verificar configurações de rede e firewall",
                    "Revisar o campo 'ultimaResposta' para identificar o problema",
                    "Verificar logs de erro completos para mais detalhes"
                })
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_008",
                Descricao = "Erro ao processar mensagem toSoft recebida via WebSocket",
                Contexto = "ProcessWebSocketMessage",
                CamposRelevantes = "ultimaResposta",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_009",
                Descricao = "Erro ao processar comando de música",
                Contexto = "ProcessMusicCommandAsync",
                CamposRelevantes = "ultimoComando, ultimaResposta",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_010",
                Descricao = "Erro ao processar comando de agendamento",
                Contexto = "ProcessAgendamentoCommandAsync",
                CamposRelevantes = "ultimoComando, ultimaResposta",
                Origem = "soft"
            },
            
            // Erros de Rede e Comunicação
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_101",
                Descricao = "Erro de conexão de rede",
                Contexto = "requisicoes HTTP",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_102",
                Descricao = "Timeout de requisição",
                Contexto = "requisicoes HTTP",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_103",
                Descricao = "Erro HTTP não tratado",
                Contexto = "requisicoes HTTP",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_104",
                Descricao = "Erro ao fazer requisição API",
                Contexto = "requisicoes HTTP",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_105",
                Descricao = "Erro ao conectar WebSocket",
                Contexto = "conexao WebSocket",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_106",
                Descricao = "Erro ao enviar mensagem WebSocket",
                Contexto = "envio WebSocket",
                CamposRelevantes = "ultimoComando, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_107",
                Descricao = "Erro ao receber mensagem WebSocket",
                Contexto = "recepcao WebSocket",
                CamposRelevantes = "ultimaResposta, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_108",
                Descricao = "Erro ao conectar UDP",
                Contexto = "conexao UDP",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_109",
                Descricao = "Erro ao enviar mensagem UDP",
                Contexto = "envio UDP",
                CamposRelevantes = "ultimoComando, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_110",
                Descricao = "Erro ao receber mensagem UDP",
                Contexto = "recepcao UDP",
                CamposRelevantes = "ultimaResposta, erroCompleto",
                Origem = "soft"
            },
            
            // Erros de Dispositivos IoT
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_201",
                Descricao = "Erro ao carregar dispositivos",
                Contexto = "carregarDispositivos",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_202",
                Descricao = "Erro ao controlar dispositivo",
                Contexto = "controlarDispositivo",
                CamposRelevantes = "ultimoComando, ultimoDispositivoAcionado",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_203",
                Descricao = "Erro ao obter status do dispositivo",
                Contexto = "obterStatusDispositivo",
                CamposRelevantes = "ultimoDispositivoAcionado",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_204",
                Descricao = "Erro ao conectar com dispositivo ESP",
                Contexto = "conectarDispositivoESP",
                CamposRelevantes = "ultimoDispositivoAcionado",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_205",
                Descricao = "Erro ao acionar dispositivo ESP",
                Contexto = "acionarDispositivoESP",
                CamposRelevantes = "ultimoComando, ultimoDispositivoAcionado",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_206",
                Descricao = "Erro ao conectar com dispositivo Ewelink",
                Contexto = "conectarDispositivoEwelink",
                CamposRelevantes = "ultimoDispositivoAcionado",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_207",
                Descricao = "Erro ao acionar dispositivo Ewelink",
                Contexto = "acionarDispositivoEwelink",
                CamposRelevantes = "ultimoComando, ultimoDispositivoAcionado",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_208",
                Descricao = "Erro ao conectar com dispositivo StarkSwitch",
                Contexto = "conectarDispositivoStarkSwitch",
                CamposRelevantes = "ultimoDispositivoAcionado",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_209",
                Descricao = "Erro ao acionar dispositivo StarkSwitch",
                Contexto = "acionarDispositivoStarkSwitch",
                CamposRelevantes = "ultimoComando, ultimoDispositivoAcionado",
                Origem = "soft"
            },
            
            // Erros de Banco de Dados Local
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_301",
                Descricao = "Erro ao acessar banco de dados local",
                Contexto = "operacoes de banco de dados",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_302",
                Descricao = "Erro ao salvar dados no banco",
                Contexto = "operacoes de banco de dados",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_303",
                Descricao = "Erro ao ler dados do banco",
                Contexto = "operacoes de banco de dados",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_304",
                Descricao = "Erro ao deletar dados do banco",
                Contexto = "operacoes de banco de dados",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            
            // Erros de UI
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_401",
                Descricao = "Erro ao carregar interface",
                Contexto = "carregamento de UI",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_402",
                Descricao = "Erro ao atualizar UI",
                Contexto = "atualizacao de UI",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_403",
                Descricao = "Erro ao navegar entre telas",
                Contexto = "navegacao",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_404",
                Descricao = "Erro ao renderizar componente",
                Contexto = "renderizacao de UI",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            
            // Erros de JSON
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_501",
                Descricao = "Erro ao parsear JSON",
                Contexto = "processamento de JSON",
                CamposRelevantes = "ultimaResposta, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_502",
                Descricao = "Erro ao serializar JSON",
                Contexto = "processamento de JSON",
                CamposRelevantes = "ultimoComando, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_503",
                Descricao = "JSON malformado",
                Contexto = "processamento de JSON",
                CamposRelevantes = "ultimaResposta, erroCompleto",
                Origem = "soft"
            },
            
            // Erros de Autenticação
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_601",
                Descricao = "Erro ao fazer login",
                Contexto = "autenticacao",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_602",
                Descricao = "Token expirado",
                Contexto = "validacao de token",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_603",
                Descricao = "Erro ao validar token",
                Contexto = "validacao de token",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_604",
                Descricao = "Erro ao fazer logout",
                Contexto = "autenticacao",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            
            // Erros de TTS/STT
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_701",
                Descricao = "Erro no Text-to-Speech",
                Contexto = "sintese de voz",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_702",
                Descricao = "Erro no Speech-to-Text",
                Contexto = "reconhecimento de voz",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_703",
                Descricao = "Erro ao inicializar reconhecimento de voz",
                Contexto = "inicializacao de STT",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_704",
                Descricao = "Erro ao processar áudio",
                Contexto = "processamento de audio",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            
            // Erros de Inicialização
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_801",
                Descricao = "Erro ao inicializar aplicativo",
                Contexto = "inicializacao",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_802",
                Descricao = "Erro ao carregar configurações",
                Contexto = "carregamento de configuracao",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_803",
                Descricao = "Erro ao inicializar serviços",
                Contexto = "inicializacao de servicos",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_804",
                Descricao = "Erro ao verificar permissões",
                Contexto = "verificacao de permissoes",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            
            // Erros Críticos Inesperados
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_999",
                Descricao = "Erro crítico inesperado",
                Contexto = "erro nao categorizado",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            
            // Códigos adicionais específicos do soft
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_007",
                Descricao = "Erro ao processar comando de aprendizado",
                Contexto = "ProcessAprendizadoCommandsAsync",
                CamposRelevantes = "ultimoComando",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_008",
                Descricao = "Erro ao processar comando local",
                Contexto = "ProcessLocalCommandsAsync",
                CamposRelevantes = "ultimoComando",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_009",
                Descricao = "Erro ao processar comando de música",
                Contexto = "ProcessMusicCommandAsync",
                CamposRelevantes = "ultimoComando, ultimaResposta",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_010",
                Descricao = "Erro ao carregar dashboard principal",
                Contexto = "LoadDashboard, CreateDashboardContent",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_011",
                Descricao = "Erro ao atualizar estatísticas do dashboard",
                Contexto = "RefreshStats",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_012",
                Descricao = "Erro ao carregar agendamentos",
                Contexto = "AgendamentosForm.LoadAgendamentos",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_013",
                Descricao = "Erro ao criar agendamento ESP",
                Contexto = "CriarAgendamentoEspForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_014",
                Descricao = "Erro ao criar agendamento Starkswitch",
                Contexto = "CriarAgendamentoStarkswitchForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_015",
                Descricao = "Erro ao criar agendamento Ewelink",
                Contexto = "CriarAgendamentoEwelinkForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_016",
                Descricao = "Erro ao deletar agendamento",
                Contexto = "AgendamentosForm.BtnDeletar_Click",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_017",
                Descricao = "Erro ao carregar dispositivos Starkswitch",
                Contexto = "DevicesForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_018",
                Descricao = "Erro ao criar dispositivo Starkswitch",
                Contexto = "DevicesForm, DeviceEditForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_019",
                Descricao = "Erro ao atualizar dispositivo Starkswitch",
                Contexto = "DeviceEditForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_020",
                Descricao = "Erro ao deletar dispositivo Starkswitch",
                Contexto = "DevicesForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_021",
                Descricao = "Erro ao carregar dispositivos ESP",
                Contexto = "DispositivosEspForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_022",
                Descricao = "Erro ao criar dispositivo ESP",
                Contexto = "DispositivosEspForm, DispositivoEspEditForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_023",
                Descricao = "Erro ao atualizar dispositivo ESP",
                Contexto = "DispositivoEspEditForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_024",
                Descricao = "Erro ao deletar dispositivo ESP",
                Contexto = "DispositivosEspForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_025",
                Descricao = "Erro ao carregar dispositivos Ewelink",
                Contexto = "DispositivosEwelinkForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_026",
                Descricao = "Erro ao carregar comandos sociais",
                Contexto = "ComandosSociaisForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_027",
                Descricao = "Erro ao criar comando social",
                Contexto = "ComandoSocialEditForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_028",
                Descricao = "Erro ao atualizar comando social",
                Contexto = "ComandoSocialEditForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_029",
                Descricao = "Erro ao deletar comando social",
                Contexto = "ComandosSociaisForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_030",
                Descricao = "Erro ao carregar aprendizados",
                Contexto = "ConfigurarAprendizadoForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_031",
                Descricao = "Erro ao criar aprendizado",
                Contexto = "AprendizadoEditForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_032",
                Descricao = "Erro ao atualizar aprendizado",
                Contexto = "AprendizadoEditForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_033",
                Descricao = "Erro ao deletar aprendizado",
                Contexto = "ConfigurarAprendizadoForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_034",
                Descricao = "Erro ao carregar alarmes/lembretes",
                Contexto = "ConfigurarAlarmesForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_035",
                Descricao = "Erro ao criar alarme/lembrete",
                Contexto = "AlarmeEditForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_036",
                Descricao = "Erro ao atualizar alarme/lembrete",
                Contexto = "AlarmeEditForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_037",
                Descricao = "Erro ao deletar alarme/lembrete",
                Contexto = "ConfigurarAlarmesForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_038",
                Descricao = "Erro ao marcar alarme como concluído",
                Contexto = "AlarmesDisparadosForm, ConfigurarAlarmesForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_039",
                Descricao = "Erro ao carregar alarmes disparados",
                Contexto = "AlarmesDisparadosForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_040",
                Descricao = "Erro ao carregar agendamentos de arquivos",
                Contexto = "ListaAgendamentosArquivosForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_041",
                Descricao = "Erro ao criar agendamento de arquivo",
                Contexto = "AgendamentoArquivoForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_042",
                Descricao = "Erro ao deletar agendamento de arquivo",
                Contexto = "ListaAgendamentosArquivosForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_043",
                Descricao = "Erro ao inicializar formulário",
                Contexto = "InitializeComponent",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_050",
                Descricao = "Erro ao realizar login",
                Contexto = "LoginForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_051",
                Descricao = "Erro ao validar credenciais salvas",
                Contexto = "BootstrapForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_052",
                Descricao = "Erro ao buscar dados do usuário atual",
                Contexto = "GetCurrentUserAsync",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_053",
                Descricao = "Erro ao atualizar dados do usuário",
                Contexto = "ConfigurarContaForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_054",
                Descricao = "Erro ao alterar senha",
                Contexto = "ConfigurarContaForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_055",
                Descricao = "Erro ao atualizar StarkCoins",
                Contexto = "AtualizarStarkCoinsAsync",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_056",
                Descricao = "Erro ao carregar planos de StarkCoins",
                Contexto = "StarkCoinsPlanosForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_057",
                Descricao = "Erro ao carregar planos ativos",
                Contexto = "PlanosAtivosForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_058",
                Descricao = "Erro ao ativar licença",
                Contexto = "LicenseActivationForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_060",
                Descricao = "Erro ao conectar WebSocket",
                Contexto = "WebSocketService",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_061",
                Descricao = "Erro ao enviar comando via WebSocket",
                Contexto = "WebSocketService",
                CamposRelevantes = "ultimoComando, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_062",
                Descricao = "Erro ao receber mensagem WebSocket",
                Contexto = "WebSocketService",
                CamposRelevantes = "ultimaResposta, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_063",
                Descricao = "Erro ao enviar comando UDP",
                Contexto = "UdpService.SendCommand",
                CamposRelevantes = "ultimoDispositivoAcionado, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_064",
                Descricao = "Erro ao receber resposta UDP",
                Contexto = "UdpService",
                CamposRelevantes = "ultimaResposta, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_065",
                Descricao = "Erro ao inicializar serviço de fala",
                Contexto = "SpeechService.Initialize",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_066",
                Descricao = "Erro ao falar texto",
                Contexto = "SpeechService.Speak",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_067",
                Descricao = "Erro ao cancelar fala",
                Contexto = "SpeechService.SpeakAsyncCancel",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_068",
                Descricao = "Erro ao inicializar reconhecimento de voz",
                Contexto = "SpeechService, WebView2",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_069",
                Descricao = "Erro ao processar texto reconhecido",
                Contexto = "CommandProcessor.ProcessCommandAsync",
                CamposRelevantes = "ultimoComando, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_070",
                Descricao = "Erro ao controlar dispositivo Ewelink",
                Contexto = "ApiService.ControlEwelinkDeviceAsync",
                CamposRelevantes = "ultimoDispositivoAcionado, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_071",
                Descricao = "Erro ao obter status de dispositivo Ewelink",
                Contexto = "ApiService.GetEwelinkDeviceStatusAsync",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_072",
                Descricao = "Erro ao publicar comando MQTT",
                Contexto = "ApiService.PublishCommandAsync",
                CamposRelevantes = "ultimoDispositivoAcionado, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_073",
                Descricao = "Erro ao verificar status Ewelink",
                Contexto = "ApiService.GetEwelinkStatusAsync",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_080",
                Descricao = "Erro de conexão com API",
                Contexto = "ApiService (qualquer requisição)",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_081",
                Descricao = "Erro de autenticação na API",
                Contexto = "ApiService (qualquer requisição autenticada)",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_082",
                Descricao = "Erro ao deserializar resposta da API",
                Contexto = "ApiService (qualquer método que deserializa)",
                CamposRelevantes = "ultimaResposta, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_083",
                Descricao = "Erro ao serializar requisição para API",
                Contexto = "ApiService (qualquer método que serializa)",
                CamposRelevantes = "ultimoComando, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_084",
                Descricao = "Erro HTTP não tratado",
                Contexto = "ApiService",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_085",
                Descricao = "Erro ao buscar previsão do tempo",
                Contexto = "ApiService.GetWeatherForecastAsync",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_086",
                Descricao = "Erro ao buscar estatísticas do usuário",
                Contexto = "ApiService.GetUserStatsAsync",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_090",
                Descricao = "Erro ao inicializar banco de dados",
                Contexto = "LocalDatabase.InitializeDatabase",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_091",
                Descricao = "Erro ao salvar comando social local",
                Contexto = "LocalDatabase.SaveComandoSocial",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_092",
                Descricao = "Erro ao salvar dispositivo ESP local",
                Contexto = "LocalDatabase.SaveDispositivoEsp",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_093",
                Descricao = "Erro ao salvar lembrete",
                Contexto = "LocalDatabase.SaveLembrete",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_094",
                Descricao = "Erro ao salvar aprendizado",
                Contexto = "LocalDatabase.SaveAprendizado",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_095",
                Descricao = "Erro ao salvar configuração",
                Contexto = "LocalDatabase.SaveSetting",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_096",
                Descricao = "Erro ao ler configuração",
                Contexto = "LocalDatabase.GetSetting",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_097",
                Descricao = "Erro ao salvar log de suporte (crítico - pode causar loop)",
                Contexto = "LocalDatabase.SaveLogToSuporte",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_100",
                Descricao = "Erro ao processar agendamentos",
                Contexto = "MainForm.ProcessarAgendamentos",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_101",
                Descricao = "Erro ao processar lembretes/alarmes",
                Contexto = "MainForm.ProcessarLembretes",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_102",
                Descricao = "Erro ao atualizar contador de alarmes",
                Contexto = "MainForm.AtualizarContadorAlarmes",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_103",
                Descricao = "Erro ao executar agendamento de arquivo",
                Contexto = "ProcessarAgendamentos (arquivos)",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_110",
                Descricao = "Erro ao carregar configurações do assistente",
                Contexto = "LocalDatabase.GetConfigAssistente",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_111",
                Descricao = "Erro ao salvar configurações do assistente",
                Contexto = "ConfigAssistenteForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_112",
                Descricao = "Erro ao verificar configuração do assistente",
                Contexto = "MainForm.VerificarConfiguracaoAssistente",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_120",
                Descricao = "Erro ao inicializar WebView2",
                Contexto = "MainForm.InitBrowser",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_121",
                Descricao = "Erro ao carregar página no WebView2",
                Contexto = "MainForm (WebView2)",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_122",
                Descricao = "Erro ao processar mensagem do WebView2",
                Contexto = "MainForm (WebView2 message handler)",
                CamposRelevantes = "ultimaResposta, erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_123",
                Descricao = "Erro ao executar script no WebView2",
                Contexto = "MainForm (WebView2)",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_130",
                Descricao = "Erro ao processar callback de pagamento",
                Contexto = "PaymentCallbackService",
                CamposRelevantes = "erroCompleto",
                Origem = "soft"
            },
            new ErrorCodeDescription
            {
                CodigoDeErro = "ERR_131",
                Descricao = "Erro ao abrir formulário de adicionar fundos",
                Contexto = "AddFundsForm",
                CamposRelevantes = "erroCompleto",
                Origem = "soft",
                Solucoes = System.Text.Json.JsonSerializer.Serialize(new List<string>
                {
                    "Verificar se o formulário está corretamente inicializado",
                    "Verificar se há dependências faltando (bibliotecas, DLLs)",
                    "Verificar permissões de acesso aos recursos do formulário",
                    "Reiniciar o aplicativo Windows Forms",
                    "Verificar logs de erro completos para identificar a causa raiz"
                })
            }
        };
    }
}

