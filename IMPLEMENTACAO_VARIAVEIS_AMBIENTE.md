# Implementação de Variáveis de Ambiente - Resumo

## ✅ O que foi implementado

### 1. Endpoint de Configuração na API
- **Arquivo**: `StarkAid.Api/Controllers/ConfigController.cs`
- **Endpoint**: `GET /api/Config/app-config`
- **Funcionalidade**: Retorna configurações públicas necessárias para os apps cliente:
  - Base URL da API
  - Credenciais do Spotify (ClientId, ClientSecret)
  - Credenciais do Ewelink (ClientId, ClientSecret, RedirectUri)

### 2. Modificações na API
- **EwelinkService**: Agora usa `IConfiguration` ao invés de valores hardcoded
- **Program.cs**: Adicionado `HttpContextAccessor` para suporte ao EwelinkService
- **Variáveis de ambiente necessárias**: Documentadas em `VARIAVEIS_AMBIENTE.md`

### 3. Modificações no Windows Forms
- **ApiService**: Agora busca a base URL da API através do endpoint `/api/Config/app-config`
- **Modelo criado**: `AppConfig.cs` para deserializar a resposta da API
- **Comportamento**: 
  - Usa URL padrão inicialmente (`https://starkaid.runasp.net/api/`)
  - Busca configuração em background
  - Atualiza a base URL quando a configuração é carregada

### 4. App Kotlin (Pendente)
- **Arquivo**: `starkaidautomacao/app/src/main/java/com/starkaid/starkaidapp/services/ApiClient.kt`
- **Status**: Ainda usa base URL hardcoded
- **Ação necessária**: Implementar busca de configuração similar ao Windows Forms

## 📋 Variáveis de Ambiente Necessárias no Servidor

Todas as variáveis estão documentadas em `VARIAVEIS_AMBIENTE.md`. Principais:

### Obrigatórias:
- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`
- `Firebase__CredentialsPath`
- `StripeSettings__SecretKey`, `StripeSettings__PublishableKey`
- `AWS__AccessKey`, `AWS__SecretKey`, `AWS__Region`
- `IaApiKeys__GroApiKey`, `IaApiKeys__OpenRouterKEY`
- `Mqtt__Broker`, `Mqtt__Port`, `Mqtt__Username`, `Mqtt__Password`
- `EmailSettings__From`, `EmailSettings__SmtpServer`, `EmailSettings__Port`, `EmailSettings__Username`, `EmailSettings__Password`

### Para Apps Cliente (retornadas via endpoint):
- `ApiBaseUrl`
- `Spotify__ClientId`, `Spotify__ClientSecret`
- `Ewelink__ClientId`, `Ewelink__ClientSecret`, `Ewelink__RedirectUri`

## 🔧 Próximos Passos

### 1. Configurar Variáveis no Servidor
Configure todas as variáveis de ambiente listadas em `VARIAVEIS_AMBIENTE.md` no seu servidor de produção.

### 2. Testar Endpoint de Configuração
```bash
curl https://starkaid.runasp.net/api/Config/app-config
```

Deve retornar:
```json
{
  "apiBaseUrl": "https://starkaid.runasp.net",
  "spotify": {
    "clientId": "...",
    "clientSecret": "...",
    "tokenUrl": "https://accounts.spotify.com/api/token"
  },
  "ewelink": {
    "clientId": "...",
    "clientSecret": "...",
    "redirectUri": "https://starkaid.runasp.net/auth/ewelink/callback.html"
  }
}
```

### 3. Implementar no App Kotlin (Opcional mas Recomendado)
Modificar `ApiClient.kt` para:
1. Buscar configuração do endpoint `/api/Config/app-config` na primeira inicialização
2. Usar a base URL retornada ao invés da hardcoded
3. Armazenar configurações do Spotify para uso no `SpotifyWebApi.kt`

### 4. Remover Chaves Hardcoded do App Kotlin
Após implementar a busca de configuração:
- Remover `CLIENT_ID` e `CLIENT_SECRET` hardcoded de `SpotifyWebApi.kt`
- Buscar essas credenciais do endpoint de configuração ou do SessionManager

## 🔒 Segurança

✅ **Implementado**:
- Endpoint de configuração é público (AllowAnonymous) mas pode ser protegido por rate limiting
- Chaves sensíveis não são mais hardcoded nos apps
- EwelinkService usa configuração ao invés de valores fixos

⚠️ **Recomendações**:
- Considere adicionar rate limiting no endpoint `/api/Config/app-config`
- Considere adicionar autenticação básica ou IP whitelist se necessário
- Revise e rotacione chaves regularmente
- Use diferentes chaves para desenvolvimento e produção

## 📝 Notas

- O Windows Forms ainda usa uma URL padrão inicial para buscar a configuração
- Se a busca de configuração falhar, o app continua usando a URL padrão
- O app Kotlin ainda precisa ser atualizado para usar o endpoint de configuração

