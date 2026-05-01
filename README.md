# StarkAid

StarkAid is a product-style automation platform built around a central `ASP.NET Core` backend, a `Blazor` web client, a Windows desktop client, and a `Kotlin` Android application.

This repository is useful as a public engineering sample because it shows more than isolated CRUD code. It demonstrates multi-client product architecture, realtime operator flows, connected-device orchestration, third-party integrations, monetization hooks, and the practical configuration work needed to move a private codebase toward a publishable state.

## Why this repository matters

- multi-project solution with backend, web, desktop, and Android clients
- authentication with `JWT`, refresh tokens, and runtime device credentials
- realtime communication with `SignalR` and WebSocket-style flows
- automation and IoT integrations for `ESP32`, `eWeLink`, and `Tuya / Thingclips`
- support tooling and operator workflows instead of tutorial-only pages
- payment and plan infrastructure with `Stripe`
- environment cleanup work to separate public code from private credentials

## Repository map

```text
StarkAid/
├── StarkAid.Api/            # ASP.NET Core backend
├── StarkAid.Web/            # Blazor web client
├── StarkAid.WindowsForms/   # Windows desktop client
├── StarkAid.AudioResolver/  # Audio helper service
└── starkaidautomacao/       # Kotlin Android client
```

## What the system does

### `StarkAid.Api`

The backend handles:

- JWT authentication and refresh-token flows
- user, device, and account management
- `ESP32` and IoT command orchestration
- `eWeLink` integration
- `Tuya / Thingclips` integration
- routines, scheduling, and remote command execution
- support and operator messaging
- payment and subscription flows
- push notifications through `Firebase`
- realtime communication with `SignalR`

### `StarkAid.Web`

The web client is built with `Blazor` and focuses on:

- admin and operational screens
- support flows
- online tools and product dashboards
- browser-based interaction with the backend

### `StarkAid.WindowsForms`

The desktop client supports local operational workflows that still matter in the product ecosystem.

### `starkaidautomacao`

The Android app is the mobile surface for:

- device control
- voice commands
- automation routines
- support chat
- connected-service interactions

### `StarkAid.AudioResolver`

This module acts as a focused helper around audio and media-related flows.

## Main stack

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

## What this repository proves technically

- product architecture across multiple clients, not just one web app
- backend integration design for devices and third-party services
- realtime operator tooling with stateful user flows
- practical environment management for public publishing
- public-facing documentation that explains private setup requirements clearly

## Local setup order

1. Configure and run `StarkAid.Api`
2. Point `StarkAid.Web` to the local API
3. Configure the Android app under `starkaidautomacao`
4. Validate login, `SignalR`, device operations, and support flows

## Requirements

- `.NET SDK 8`
- `SQL Server`
- `Android Studio`
- `JDK 17`
- `WebView2 Runtime` for the Windows client

## API configuration

The backend reads configuration from:

- `appsettings.json`
- `appsettings.{Environment}.json`
- environment variables

Template file:

- [StarkAid.Api/appsettings-template.json](StarkAid.Api/appsettings-template.json)

### Bootstrap steps

1. Copy `StarkAid.Api/appsettings-template.json` to `StarkAid.Api/appsettings.Development.json`, or use `dotnet user-secrets` and environment variables.
2. Set the SQL Server connection string.
3. Make sure the Firebase credentials file path exists on the local machine.
4. Apply migrations and start the API.

### Useful commands

```powershell
dotnet restore
dotnet ef database update --project StarkAid.Api
dotnet run --project StarkAid.Api
```

### Expected environment variables and keys

Use `__` in environment variable names to map nested JSON sections in .NET.

| Key | Required | Purpose |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | Yes | Main SQL Server database |
| `Jwt__Key` | Yes | JWT signing key |
| `Jwt__Issuer` | Yes | JWT issuer |
| `Jwt__Audience` | Yes | JWT audience |
| `Firebase__CredentialsPath` | Yes | Path to `firebase-adminsdk.json` |
| `Tuya__AccessId` | If using Tuya | Tuya API credential |
| `Tuya__AccessSecret` | If using Tuya | Tuya API secret |
| `Tuya__BaseUrl` | If using Tuya | Tuya API base URL |
| `Tuya__CountryCode` | If using Tuya | Country code used by the integration |
| `WppConnectOptions__BaseUrl` | If using WPP | Base endpoint for the WhatsApp service |
| `WppConnectOptions__TokenDeAutenticacao` | If using WPP | Internal token for the WPP service |
| `WppConnectOptions__NovoDominio` | Optional | Alternate WPP domain |
| `WppConnectOptions__UserId` | Optional | Technical user for WPP |
| `NlpConnectOptions__BaseUrl` | If using external NLP | Base endpoint for the NLP service |
| `NlpConnectOptions__TokenDeAutenticacao` | If using external NLP | NLP service token |
| `NlpConnectOptions__NovoDominio` | Optional | Alternate NLP domain |
| `NlpConnectOptions__UserId` | Optional | Technical user for NLP |
| `AiTelemetry__CostPer1KTokens` | Optional | Reference cost for telemetry |
| `AiTelemetry__DefaultTokensPerInteraction` | Optional | Default token estimate per interaction |
| `IaApiKeys__GroApiKey` | If using AI provider | Groq API key |
| `IaApiKeys__OpenRouterKEY` | If using AI provider | OpenRouter API key |
| `AWS__AccessKey` | If using transcription | AWS access key |
| `AWS__SecretKey` | If using transcription | AWS secret key |
| `AWS__Profile` | Optional | Local AWS profile |
| `AWS__Region` | If using transcription | AWS region |
| `Spotify__ClientId` | If using Spotify | Spotify application client ID |
| `Spotify__ClientSecret` | If using Spotify | Spotify application client secret |
| `Spotify__RedirectUri` | If using Spotify | Spotify OAuth redirect URI |
| `StripeSettings__SecretKey` | If using billing | Stripe secret key |
| `StripeSettings__PublishableKey` | If using billing | Stripe publishable key |
| `StripeSettings__WebhookSecret` | If using billing | Stripe webhook secret |
| `StripeSettings__PriceIdNivel2` through `StripeSettings__PriceIdNivel7` | If using billing | Price and product IDs |
| `StripeSettings__CheckoutFrontendUrl` | If using billing | Checkout frontend URL |
| `StripeSettings__AppDeepLink` | Optional | Android deep link |
| `StripeSettings__SoftwareDeepLink` | Optional | Desktop client deep link |
| `Mqtt__Broker` | If using ESP32 or MQTT | MQTT broker address |
| `Mqtt__Port` | If using ESP32 or MQTT | MQTT broker port |
| `Mqtt__Username` | If using ESP32 or MQTT | MQTT username |
| `Mqtt__Password` | If using ESP32 or MQTT | MQTT password |
| `EmailSettings__From` | If using email | Sender address |
| `EmailSettings__SmtpServer` | If using email | SMTP server |
| `EmailSettings__Port` | If using email | SMTP port |
| `EmailSettings__Username` | If using email | SMTP username |
| `EmailSettings__Password` | If using email | SMTP password |
| `Ewelink__ClientId` | If using eWeLink | eWeLink client ID |
| `Ewelink__ClientSecret` | If using eWeLink | eWeLink client secret |
| `Ewelink__RedirectUri` | If using eWeLink | eWeLink redirect URI |
| `YouTube__ApiKey` | If using music search | YouTube API key |

### Operational notes

- The system uses `JWT` and a runtime `Api-Key` per user or device after authentication.
- That `Api-Key` is not a build secret. It is generated by the backend during login or device registration.
- Before any deeper public expansion, the right next step is credential rotation for every real provider that has ever been used by this product.

## Web client configuration

Template file:

- [StarkAid.Web/wwwroot/appsettings-template.json](StarkAid.Web/wwwroot/appsettings-template.json)

### Setup steps

1. Copy `StarkAid.Web/wwwroot/appsettings-template.json` to `StarkAid.Web/wwwroot/appsettings.json`.
2. Set `Api:BaseUrl` to the API you want the client to use.
3. If the `eWeLink` browser flow is enabled, set `Ewelink:ClientId` and `Ewelink:ClientSecret`.
4. Run the web client.

```powershell
dotnet restore
dotnet run --project StarkAid.Web
```

### Important note about secrets in Blazor WebAssembly

`StarkAid.Web` runs in the browser. Any secret placed in `wwwroot/appsettings.json` is visible to the client. If a provider value is truly secret, it should move to the backend instead of staying in the browser configuration.

## Android configuration

The Android app has dedicated setup documentation here:

- [starkaidautomacao/README.md](starkaidautomacao/README.md)

At a high level, local setup expects:

- `google-services.json`
- `Thingclips / Tuya` identifiers
- `AdMob` and `Unity Ads` IDs
- backend `REST`, `SignalR`, `WebSocket`, and Spotify callback URLs
- Spotify application credentials if the flow remains client-side

## Windows client

The Windows client depends on the backend being configured and on `WebView2 Runtime` being installed locally.

## AudioResolver

`StarkAid.AudioResolver` currently has a lightweight configuration surface and mainly relies on logging settings.

## Public deployment reference

- Web and API: [starkaidautomacao.runasp.net](https://starkaidautomacao.runasp.net/)

## Repository status

This is real product code, not tutorial code. It has already gone through an initial sanitization pass for public exposure, but the long-term professionalization path still includes:

- broader credential rotation
- deeper cleanup of historic artifacts
- tighter separation between client-safe and server-only configuration
- clearer deployment automation

Additional notes:

- [GITHUB_CLEANUP_NOTES.md](GITHUB_CLEANUP_NOTES.md)

## License

See `LICENSE`.
