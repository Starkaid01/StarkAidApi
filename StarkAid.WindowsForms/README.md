# StarkAid Windows Forms

Software de automação residencial com interface futurista para Windows Forms.

## Funcionalidades

### Autenticação
- Login com email e senha
- Cadastro de novos usuários (em desenvolvimento)
- Gerenciamento de token JWT

### Dashboard
- Exibição de StarkCoins
- Total de dispositivos
- Total de comandos sociais
- Status da API
- Status do MQTT

### Comandos Sociais
- Criar, editar e deletar comandos sociais
- Armazenamento local em SQLite
- Sincronização com API

### Dispositivos StarkSwitch
- Criar, editar e deletar dispositivos
- Botão ligar/desligar (via MQTT)

### Dispositivos ESP
- Criar, editar e deletar dispositivos ESP
- Configuração de IP, porta e comando
- Botão ligar/desligar
- Comunicação via UDP

### Chat por Voz
- Reconhecimento de voz em português
- Comandos locais do Windows:
  - "Que horas são"
  - "Que dia é hoje"
  - "Abra calculadora"
  - "Abra Facebook"
  - "Abra YouTube"
  - "Abra meus documentos"
  - "Esvazie a lixeira"
- Comandos sociais personalizados
- Comandos de dispositivos ESP
- Integração com Super IA quando ativada

### Comunicação
- WebSocket (SignalR) para receber comandos da API
- UDP para comunicação com dispositivos ESP
- Envio de respostas UDP para API

### Sincronização
- Sincronização automática ao iniciar (se houver internet)
- Botão manual para atualizar dados
- Limpeza e atualização de bancos locais

## Tecnologias

- .NET 8.0
- Windows Forms
- SQLite (banco local)
- SignalR Client (WebSocket)
- System.Speech (reconhecimento e síntese de voz)
- NAudio (áudio)
- HttpClient (comunicação com API)

## Configuração

1. A URL base da API está configurada como: `https://starkaid.runasp.net/api`
2. O banco de dados local é criado em: `%LocalAppData%\StarkAid\local.db`
3. O WebSocket conecta em: `https://starkaid.runasp.net/dispositivoesphub?type=software`
4. O listener UDP usa a porta padrão: `8888`

## Como Usar

1. Execute o aplicativo
2. Faça login com suas credenciais
3. No dashboard, você pode:
   - Ver suas estatísticas
   - Ativar/desativar a IA de voz
   - Atualizar dados manualmente
4. Navegue pelos menus para gerenciar:
   - Comandos sociais
   - Dispositivos StarkSwitch
   - Dispositivos ESP
   - Configurações do usuário

## Interface

A interface foi projetada com um tema futurista:
- Cores escuras (preto/cinza escuro)
- Acentos em ciano/verde
- Efeitos sonoros em cliques
- Animações suaves

## Notas

- O reconhecimento de voz requer o pacote de idioma português do Windows
- A síntese de voz usa a voz padrão do sistema
- A comunicação UDP é bidirecional (envio e recebimento)
- O WebSocket mantém conexão persistente com a API

