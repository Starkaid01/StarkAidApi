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

Voce precisa fornecer:

- `app/google-services.json`

Sem esse arquivo, recursos como `Firebase Messaging` e `Analytics` nao vao inicializar corretamente.

### 2. Thingclips / Tuya

Hoje os identificadores estao declarados diretamente em:

- `app/src/main/AndroidManifest.xml`

Voce precisa revisar ou substituir:

- `THING_SMART_APPKEY`
- `THING_SMART_SECRET`

### 3. AdMob e Unity Ads

Tambem estao no `AndroidManifest.xml`:

- `com.google.android.gms.ads.APPLICATION_ID`
- `unityads.appid`

Se voce nao usar monetizacao em ambiente local, pode manter placeholders ou desativar esses SDKs no seu fork.

### 4. URLs do backend

O app hoje usa endpoints hardcoded em codigo-fonte. Para rodar com seu proprio backend, revise estes arquivos:

- `app/src/main/java/com/starkaid/starkaidapp/services/ApiClient.kt`
  - URL base REST da API
- `app/src/main/java/com/starkaid/starkaidapp/services/HubService.kt`
  - URL do hub `SignalR`
- `app/src/main/java/com/starkaid/starkaidapp/services/RefreshTokenInterceptor.kt`
  - endpoint de refresh token
- `app/src/main/java/com/starkaid/starkaidapp/services/WebSocketManager.kt`
  - endpoint `WebSocket`
- `app/src/main/java/com/starkaid/starkaidapp/ui/QrActivityWppConnect.kt`
  - URL do fluxo QR / WPP
- `app/src/main/java/com/starkaid/starkaidapp/ui/MainActivity.kt`
  - URL de callback / troca de codigo do Spotify com backend

### 5. Spotify

Hoje as credenciais do app Spotify aparecem em codigo-fonte. Revise:

- `app/src/main/java/com/starkaid/starkaidapp/services/SpotifyService.kt`
- `app/src/main/java/com/starkaid/starkaidapp/ui/MainActivity.kt`

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
2. Garanta que `app/google-services.json` exista.
3. Ajuste as chaves do `Thingclips / Tuya` no `AndroidManifest.xml`.
4. Revise IDs de anuncios no `AndroidManifest.xml`.
5. Troque todas as URLs hardcoded para apontarem para sua API.
6. Revise o fluxo do Spotify.
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
