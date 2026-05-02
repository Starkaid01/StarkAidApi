# StarkAid Automacao

Aplicativo Android em `Kotlin` para automacao residencial, assistencia por voz, integracao com dispositivos e operacao movel do ecossistema StarkAid.

Este subprojeto vive dentro do monorepo principal e conversa com a `StarkAid.Api` para autenticacao, sincronizacao, dispositivos, suporte, pagamentos e servicos online.

## O que o app cobre

- controle de dispositivos `ESP32`
- integracao com `eWeLink`
- assistente por voz
- rotinas, lembretes e agendamentos
- suporte em tempo real
- Spotify e recursos de audio
- notificacoes push com `Firebase`
- SignalR e WebSocket

## Stack

- `Kotlin`
- `Android SDK 26+`
- `Jetpack Compose`
- `ViewBinding`
- `Room`
- `Retrofit`
- `OkHttp`
- `Firebase Analytics / Messaging`
- `SignalR Client`
- `AWS Transcribe Streaming`
- `ExoPlayer`

## Dependencia de backend

Backend relacionado neste repositorio:

- [StarkAid.Api](../StarkAid.Api)

O app usa a API para:

- login e refresh token
- leitura de configuracao remota
- dispositivos e comandos
- suporte e telemetria
- planos, checkout e saldo
- integracoes de terceiros

## Requisitos

- `Android Studio`
- `JDK 17`
- `Android SDK`
- `Gradle Wrapper`
- backend StarkAid funcional

## Arquivos locais obrigatorios

### 1. Firebase

Forneca:

- `app/google-services.json`

Sem esse arquivo, `Firebase Messaging` e partes do Analytics nao inicializam corretamente.

No repositorio publico, o build continua compilando sem esse arquivo. Nesse caso, o plugin `google-services` nao e aplicado e a geracao de recursos do Firebase fica desativada.

### 2. Configuracao local do app

Copie:

- `starkaid.local.properties.example`

para:

- `starkaid.local.properties`

Esse arquivo e lido pelo build e preenche os defaults do `BuildConfig`.

## Chaves e variaveis de configuracao

### Ambiente

- `STARKAID_IS_DEVELOPMENT`
- `STARKAID_DEV_API_BASE_URL`
- `STARKAID_DEV_WEB_BASE_URL`
- `STARKAID_PROD_API_BASE_URL`
- `STARKAID_PROD_WEB_BASE_URL`

Esses valores alimentam:

- `app/src/main/java/com/starkaid/starkaidapp/config/ApiConfig.kt`

`ApiConfig` define as URLs default de API e web. Depois do login, o app ainda pode carregar configuracao remota do backend e salvar no `SessionManager`.

### Spotify

- `STARKAID_SPOTIFY_CLIENT_ID`
- `STARKAID_SPOTIFY_CLIENT_SECRET`

Esses valores sao fallback local. Se o backend devolver configuracao atualizada pelo endpoint de app-config, o app persiste isso em runtime.

Redirect URI atual do app:

- `starkaid://spotifycallback`

O aplicativo cadastrado no Spotify precisa aceitar esse retorno.

### eWeLink

- `STARKAID_EWELINK_CLIENT_ID`
- `STARKAID_EWELINK_CLIENT_SECRET`

Esses valores sao usados pelo fluxo mobile de autenticacao eWeLink. No estado atual, eles entram no `BuildConfig` a partir de `starkaid.local.properties`. Para um endurecimento maior, o ideal e mover esse fluxo totalmente para a API.

### Ads

- `ADMOB_APP_ID`
- `UNITY_ADS_APP_ID`

Esses valores sao injetados no `AndroidManifest.xml` via `manifestPlaceholders`.

## Fluxo de configuracao

1. O build gera defaults locais a partir de `starkaid.local.properties`.
2. `ApiConfig` expõe as URLs e fallbacks do app.
3. `ApiClient` pode buscar configuracao remota em `/api/v1/Config/app-config`.
4. `SessionManager` salva overrides dinamicos como base URL, Spotify e eWeLink.
5. Os servicos usam primeiro a configuracao salva em runtime e depois caem no fallback local.

## Build

No Windows:

```powershell
.\gradlew.bat assembleDebug
```

Ou release:

```powershell
.\gradlew.bat assembleRelease
```

## Passo a passo para subir

1. Abra a pasta `starkaidautomacao` no Android Studio.
2. Se for usar notificacoes push e analytics reais, garanta que `app/google-services.json` exista.
3. Copie `starkaid.local.properties.example` para `starkaid.local.properties`.
4. Preencha as URLs de ambiente, os fallbacks do Spotify e os IDs de ads.
5. Se for usar desenvolvimento local, ajuste `STARKAID_IS_DEVELOPMENT=true` e as URLs `DEV`.
6. Rode sync do Gradle.
7. Gere o build e execute em emulador ou dispositivo fisico.

## Observacoes

- Este subprojeto ja usa configuracao remota do backend; o arquivo local serve como base segura e previsivel para bootstrap.
- O `google-services.json` e opcional para compilar o projeto publico, mas necessario para recursos reais de `Firebase Messaging` e Analytics.
- O fluxo do Spotify ainda depende de segredo no cliente como fallback. O ideal de longo prazo e mover isso 100% para o backend.
- O `README` anterior deste subprojeto estava desalinhado com o codigo real. Este documento reflete o fluxo atual do monorepo.
