# StarkAid

Plataforma de automacao residencial e assistencia inteligente com backend central, cliente web, cliente desktop e aplicativo Android.

O repositorio concentra o ecossistema completo do produto, incluindo autenticacao, controle de dispositivos, comandos por voz, mensageria, integracoes de terceiros, cobranca e suporte em tempo real.

## Modulos

### `StarkAid.Api`

Backend em `ASP.NET Core` responsavel por:

- autenticacao JWT e refresh token
- gerenciamento de usuarios e dispositivos
- controle de dispositivos `ESP32`
- integracao com `eWeLink`
- integracao com `Tuya / Thingclips`
- comandos sociais, rotinas e agendamentos
- pagamentos e planos com `Stripe`
- notificacoes push com `Firebase`
- telemetria, IA e suporte em tempo real
- `WebSocket` e `SignalR`

### `StarkAid.Web`

Front-end web em `.NET / Blazor` para operacao administrativa, paineis, suporte e recursos online do sistema.

### `StarkAid.WindowsForms`

Cliente desktop em `.NET` para uso local e cenarios operacionais especificos.

### `starkaidautomacao`

Aplicativo Android em `Kotlin` para automacao, controle por voz e operacao movel do ecossistema.

### `StarkAid.AudioResolver`

Servico auxiliar para fluxos relacionados a audio e processamento de midia.

## Stack principal

- `.NET 8`
- `ASP.NET Core`
- `Blazor`
- `Entity Framework Core`
- `SQL Server`
- `SignalR`
- `JWT`
- `Serilog`
- `Kotlin`
- `Android SDK`
- `Room`
- `Retrofit`
- `Firebase`
- `MQTT`
- `AWS Transcribe`
- `Stripe`

## Estrutura

```text
StarkAid/
├── StarkAid.Api/            # API principal
├── StarkAid.Web/            # Front web / Blazor
├── StarkAid.WindowsForms/   # Cliente desktop
├── StarkAid.AudioResolver/  # Servico auxiliar de audio
└── starkaidautomacao/       # App Android Kotlin
```

## Ordem recomendada para subir localmente

1. Configurar e subir `StarkAid.Api`
2. Configurar `StarkAid.Web` apontando para a API local
3. Ajustar o app Android `starkaidautomacao`
4. Validar SignalR, WebSocket, login e integracoes externas

## Requisitos gerais

- `.NET SDK 8`
- `SQL Server`
- `Android Studio`
- `JDK 17`
- `WebView2 Runtime` para o cliente Windows

## Configuracao da API

O backend le configuracao de:

- `appsettings.json`
- `appsettings.{Environment}.json`
- variaveis de ambiente

Arquivo de exemplo:

- [StarkAid.Api/appsettings-template.json](StarkAid.Api/appsettings-template.json)

### Passos

1. Copie `StarkAid.Api/appsettings-template.json` para `StarkAid.Api/appsettings.Development.json` ou use `dotnet user-secrets` / variaveis de ambiente.
2. Ajuste a string de conexao SQL Server.
3. Garanta que o caminho do arquivo de credenciais do Firebase exista na maquina.
4. Rode migrations e suba a API.

### Comandos uteis

```powershell
dotnet restore
dotnet ef database update --project StarkAid.Api
dotnet run --project StarkAid.Api
```

### Variaveis de ambiente e chaves esperadas

Use nomes com `__` para mapear secoes JSON no .NET.

| Chave | Obrigatoria | Uso |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | Sim | Banco principal SQL Server |
| `Jwt__Key` | Sim | Assinatura dos tokens JWT |
| `Jwt__Issuer` | Sim | Emissor dos tokens |
| `Jwt__Audience` | Sim | Audiencia dos tokens |
| `Firebase__CredentialsPath` | Sim | Caminho do `firebase-adminsdk.json` |
| `Tuya__AccessId` | Se usar Tuya | Credencial da API Tuya |
| `Tuya__AccessSecret` | Se usar Tuya | Segredo da API Tuya |
| `Tuya__BaseUrl` | Se usar Tuya | Base URL da API Tuya |
| `Tuya__CountryCode` | Se usar Tuya | Codigo de pais usado na integracao |
| `WppConnectOptions__BaseUrl` | Se usar WPP | Endpoint base do servico de WhatsApp |
| `WppConnectOptions__TokenDeAutenticacao` | Se usar WPP | Token interno do servico WPP |
| `WppConnectOptions__NovoDominio` | Opcional | Dominio alternativo para WPP |
| `WppConnectOptions__UserId` | Opcional | Usuario tecnico do WPP |
| `NlpConnectOptions__BaseUrl` | Se usar NLP externo | Endpoint base do servico NLP |
| `NlpConnectOptions__TokenDeAutenticacao` | Se usar NLP externo | Token do servico NLP |
| `NlpConnectOptions__NovoDominio` | Opcional | Dominio alternativo do NLP |
| `NlpConnectOptions__UserId` | Opcional | Usuario tecnico do NLP |
| `AiTelemetry__CostPer1KTokens` | Opcional | Custo de referencia para telemetria |
| `AiTelemetry__DefaultTokensPerInteraction` | Opcional | Estimativa padrao de tokens |
| `IaApiKeys__GroApiKey` | Se usar IA | Chave do provedor Groq |
| `IaApiKeys__OpenRouterKEY` | Se usar IA | Chave do OpenRouter |
| `AWS__AccessKey` | Se usar transcricao | Credencial AWS |
| `AWS__SecretKey` | Se usar transcricao | Segredo AWS |
| `AWS__Profile` | Opcional | Perfil AWS local |
| `AWS__Region` | Se usar transcricao | Regiao AWS |
| `Spotify__ClientId` | Se usar Spotify | Client ID do app Spotify |
| `Spotify__ClientSecret` | Se usar Spotify | Client secret do app Spotify |
| `Spotify__RedirectUri` | Se usar Spotify | Redirect URI do OAuth Spotify |
| `StripeSettings__SecretKey` | Se usar cobranca | Secret key do Stripe |
| `StripeSettings__PublishableKey` | Se usar cobranca | Publishable key do Stripe |
| `StripeSettings__WebhookSecret` | Se usar cobranca | Secret do webhook do Stripe |
| `StripeSettings__PriceIdNivel2` ate `StripeSettings__PriceIdNivel7` | Se usar cobranca | IDs dos produtos/precos |
| `StripeSettings__CheckoutFrontendUrl` | Se usar cobranca | URL da tela de checkout |
| `StripeSettings__AppDeepLink` | Opcional | Deep link do app Android |
| `StripeSettings__SoftwareDeepLink` | Opcional | Deep link do cliente desktop |
| `Mqtt__Broker` | Se usar ESP32/MQTT | Endereco do broker MQTT |
| `Mqtt__Port` | Se usar ESP32/MQTT | Porta do broker MQTT |
| `Mqtt__Username` | Se usar ESP32/MQTT | Usuario MQTT |
| `Mqtt__Password` | Se usar ESP32/MQTT | Senha MQTT |
| `EmailSettings__From` | Se usar email | Email remetente |
| `EmailSettings__SmtpServer` | Se usar email | Servidor SMTP |
| `EmailSettings__Port` | Se usar email | Porta SMTP |
| `EmailSettings__Username` | Se usar email | Usuario SMTP |
| `EmailSettings__Password` | Se usar email | Senha SMTP |
| `Ewelink__ClientId` | Se usar eWeLink | Client ID eWeLink |
| `Ewelink__ClientSecret` | Se usar eWeLink | Client secret eWeLink |
| `Ewelink__RedirectUri` | Se usar eWeLink | Redirect URI eWeLink |
| `YouTube__ApiKey` | Se usar busca de musica | Chave da API do YouTube |

### Observacoes operacionais

- O sistema trabalha com `JWT` e com `Api-Key` por usuario/dispositivo em tempo de execucao.
- A `Api-Key` nao e uma credencial de build. Ela e gerada e devolvida pelo backend durante autenticacao/registro.
- O projeto hoje ainda possui segredos em arquivos versionados e historico local. Antes de abrir o repositorio publicamente, o correto e rotacionar todas as credenciais reais.

## Configuracao do front web

Arquivo de exemplo:

- [StarkAid.Web/wwwroot/appsettings-template.json](StarkAid.Web/wwwroot/appsettings-template.json)

### Passos

1. Copie `StarkAid.Web/wwwroot/appsettings-template.json` para `StarkAid.Web/wwwroot/appsettings.json`.
2. Defina `Api:BaseUrl` apontando para a API que voce vai usar.
3. Se o fluxo de eWeLink estiver habilitado no cliente, preencha `Ewelink:ClientId` e `Ewelink:ClientSecret`.
4. Rode o front web.

```powershell
dotnet restore
dotnet run --project StarkAid.Web
```

### Observacao importante sobre segredos no Blazor WASM

`StarkAid.Web` roda no navegador. Qualquer valor colocado em `wwwroot/appsettings.json` fica visivel para o cliente final. Se `ClientSecret` de terceiros for realmente secreto, o ideal e mover esse fluxo para a API e nao expor o segredo no front.

## Configuracao do Android

O aplicativo Android tem documentacao propria aqui:

- [starkaidautomacao/README.md](starkaidautomacao/README.md)

Resumo do que precisa para subir:

- `google-services.json`
- identificadores do `Thingclips / Tuya`
- IDs de `AdMob` e `Unity Ads`
- URLs do backend `REST`, `SignalR`, `WebSocket` e `Spotify callback`
- credenciais do app Spotify se o fluxo continuar client-side

## Cliente Windows

O cliente Windows depende do backend ja configurado e do `WebView2 Runtime` instalado na maquina.

## AudioResolver

`StarkAid.AudioResolver` nao exige, neste momento, um arquivo de configuracao complexo. O `appsettings.json` atual carrega apenas niveis de log.

## Deploy publico atualmente apontado no codigo

- Web/API: [starkaidautomacao.runasp.net](https://starkaidautomacao.runasp.net/)

## Estado do repositorio

Este repositorio contem codigo real de produto e ainda precisa de saneamento tecnico antes de uma exposicao publica limpa. Os principais pontos pendentes estao resumidos em:

- [GITHUB_CLEANUP_NOTES.md](GITHUB_CLEANUP_NOTES.md)

## Licenca

Ver arquivo `LICENSE`.
