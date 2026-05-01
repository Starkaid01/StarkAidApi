# StarkAid Automacao

Aplicativo Android em `Kotlin` para automacao residencial, assistencia por voz e controle de dispositivos conectados.

O app funciona como cliente movel do ecossistema StarkAid e depende de um backend compativel para login, sincronizacao, dispositivos, suporte, planos e servicos online.

## O que o app faz

- controla dispositivos `ESP32`
- integra dispositivos `eWeLink`
- integra dispositivos `Tuya / Thingclips`
- executa comandos por voz
- envia comandos sociais e automacoes personalizadas
- gerencia rotinas, agendamentos e disparos
- exibe notificacoes e alertas
- integra recursos de audio, Spotify e midia
- suporta chat e suporte em tempo real

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
- `Thingclips / Tuya SDK`

## Estrutura principal

```text
app/src/main/java/com/starkaid/starkaidapp
├── adapters/        # adapters e listas de dispositivos
├── data/            # banco local e sessao
├── iot/tuya/        # integracao com dispositivos Tuya
├── models/          # DTOs, entidades e modelos de dominio do app
├── services/        # voz, websocket, APIs, Spotify, notificacoes, IA
├── ui/              # activities e telas principais
├── util/            # utilitarios e helpers
└── viewmodels/      # view models e factories
```

## Dependencia de backend

Este aplicativo depende de um backend StarkAid compativel para:

- login
- cadastro
- renovacao de token
- sincronizacao de usuario
- comandos remotos
- suporte
- planos e licenciamento
- telemetria e servicos online

Backend relacionado neste repositorio:

- [StarkAid.Api](../StarkAid.Api)

## Requisitos para abrir localmente

- `Android Studio`
- `JDK 17`
- `Android SDK`
- `Gradle Wrapper`
- backend StarkAid funcional

## Arquivos e chaves obrigatorias

### 1. Firebase

Para recursos de `Firebase Messaging` e `Analytics`, forneca:

- `app/google-services.json`

Sem esse arquivo, o projeto ainda compila, mas o build desativa a etapa do Google Services e o app sobe sem inicializar Firebase.

### 2. Configuracao local do app

Copie:

- `starkaid.local.properties.example`

para:

- `starkaid.local.properties`

Esse arquivo alimenta o `BuildConfig` e o `AndroidManifest` com:

- `STARKAID_IS_DEVELOPMENT`
- `STARKAID_DEV_API_BASE_URL`
- `STARKAID_DEV_WEB_BASE_URL`
- `STARKAID_PROD_API_BASE_URL`
- `STARKAID_PROD_WEB_BASE_URL`
- `STARKAID_SPOTIFY_CLIENT_ID`
- `STARKAID_SPOTIFY_CLIENT_SECRET`
- `STARKAID_SPOTIFY_REDIRECT_URI`
- `ADMOB_APP_ID`
- `UNITY_ADS_APP_ID`

### 3. AdMob e Unity Ads

Os IDs de anuncios agora sao lidos por placeholder de manifesto. Se voce nao usar monetizacao em ambiente local, mantenha os placeholders do `starkaid.local.properties`.

### 4. URLs do backend

As URLs principais saem de `BuildConfig` via:

- `app/src/main/java/com/starkaid/starkaidapp/config/ApiConfig.kt`

Com isso, `ApiClient`, `HubService`, `RefreshTokenInterceptor`, `WebSocketManager`, `QrActivityWppConnect` e fluxos ligados ao backend passam a respeitar o ambiente configurado.

### 5. Spotify

O fallback de Spotify agora vem do `starkaid.local.properties`, enquanto o backend ainda pode sobrescrever `clientId` e `clientSecret` em runtime pelo endpoint de configuracao.

Voce vai precisar de:

- `Spotify Client ID`
- `Spotify Client Secret`
- `Redirect URI` compativel com o scheme do app

O caminho mais seguro e mover esse fluxo para o backend e evitar segredo em app cliente.

### 6. local.properties

Crie ou ajuste o `local.properties` na raiz do projeto com o caminho do Android SDK:

```properties
sdk.dir=C:\\Users\\SeuUsuario\\AppData\\Local\\Android\\Sdk
```

## Passo a passo para subir

1. Abra a pasta no Android Studio.
2. Copie `starkaid.local.properties.example` para `starkaid.local.properties`.
3. Ajuste URLs, Spotify e IDs de ads nesse arquivo.
4. Se for usar Firebase, adicione `app/google-services.json`.
5. Revise o fluxo do Spotify.
6. Se precisar de integracoes adicionais, confira as configuracoes retornadas pelo backend em `api/v1/Config/app-config`.
7. Gere o build e rode em emulador ou dispositivo fisico.

## Build

No Windows:

```powershell
.\gradlew.bat assembleDebug
```

Ou para release:

```powershell
.\gradlew.bat assembleRelease
```

## Autenticacao em runtime

Depois do login, o app trabalha com:

- `JWT` no header `Authorization`
- `Api-Key` devolvida pelo backend e persistida em sessao
- `FCM token` para notificacoes push

Esses dados sao gerados/obtidos em runtime. Eles nao substituem as chaves de build e integracoes de terceiros listadas acima.

## Observacao importante

Este projeto ainda possui IDs e segredos embutidos no cliente. Para uma publicacao mais profissional no GitHub, o ideal e:

1. mover configuracoes sensiveis para `gradle.properties`, `local.properties`, `BuildConfig` ou backend
2. remover artefatos de build e arquivos gerados
3. rotacionar credenciais reais ja usadas
