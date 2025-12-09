# StarkAid - Sistema de Automação Residencial

Sistema completo de automação residencial com suporte para múltiplas plataformas.

## Projetos Incluídos

### 1. StarkAid.Api
API REST desenvolvida em ASP.NET Core para gerenciar dispositivos, comandos sociais, agendamentos e integrações.

**Principais Funcionalidades:**
- Gerenciamento de dispositivos (ESP, Ewelink, Starkswitch)
- Comandos sociais personalizados
- Sistema de agendamentos
- Integração com Ewelink
- Sistema de licenças
- Chat de suporte
- Notificações
- Previsão do tempo

### 2. StarkAid.WindowsForms
Aplicação desktop Windows Forms com suporte offline completo.

**Principais Funcionalidades:**
- ✅ **Modo Offline Completo**: Funciona sem conexão com a internet
- Verificação de status da API (não verifica internet)
- Sincronização automática de dados quando online
- Armazenamento local de:
  - Dados do usuário (ID, nome, email, StarkCoins)
  - Comandos sociais
  - Dispositivos ESP
  - Dispositivos Ewelink
  - Dispositivos Starkswitch
- Comandos ESP funcionam offline (via UDP)
- Comandos sociais funcionam offline
- UI mantém último valor de StarkCoins quando offline
- Edição/exclusão desabilitada quando API offline

**Tecnologias:**
- .NET Windows Forms
- SQLite (banco local)
- WebView2
- WebSocket
- UDP para comunicação com dispositivos ESP

### 3. starkaidautomacao
Aplicativo Android desenvolvido em Kotlin para controle de dispositivos e automação.

**Principais Funcionalidades:**
- Controle de dispositivos ESP, Ewelink e Starkswitch
- Comandos sociais
- Agendamentos
- Chat de suporte
- Notificações

## Estrutura do Repositório

```
StarkAid/
├── StarkAid.Api/              # API REST ASP.NET Core
├── StarkAid.WindowsForms/      # Aplicação Desktop Windows
└── starkaidautomacao/         # App Android Kotlin
```

## Requisitos

### StarkAid.Api
- .NET 8.0 ou superior
- SQL Server ou SQLite
- MQTT Broker (para dispositivos Starkswitch)

### StarkAid.WindowsForms
- .NET 8.0 ou superior
- Windows 10/11
- WebView2 Runtime

### starkaidautomacao
- Android Studio
- Gradle
- Android SDK

## Configuração

### API
1. Configure a string de conexão no `appsettings.json`
2. Configure as chaves de API necessárias (Firebase, Stripe, etc.)
3. Execute as migrations do Entity Framework

### Windows Forms
1. O banco de dados local é criado automaticamente
2. Configure as credenciais na primeira execução
3. Ative uma licença

### Android
1. Configure o `local.properties` com o caminho do SDK
2. Configure as credenciais da API no código

## Funcionamento Offline (Windows Forms)

O sistema foi projetado para funcionar completamente offline:

1. **Ao abrir o app:**
   - Verifica status da API
   - Se online: sincroniza todos os dados
   - Se offline: usa dados locais salvos

2. **Quando online:**
   - Sincroniza usuário, comandos sociais, dispositivos ESP, Ewelink e Starkswitch
   - Atualiza StarkCoins
   - Verifica licença
   - Conecta WebSocket (chat)

3. **Quando offline:**
   - Usa dados locais para exibição
   - Comandos ESP funcionam via UDP
   - Comandos sociais funcionam do banco local
   - Edição/exclusão desabilitada
   - IA e chat desabilitados (requerem API)

4. **Quando volta online:**
   - Verifica licença automaticamente
   - Sincroniza dados
   - Reconecta WebSocket
   - Reativa funcionalidades que dependem da API

## Licença

Ver arquivo LICENSE.txt

## Contribuição

Este é um projeto privado. Para contribuições, entre em contato com os mantenedores.

