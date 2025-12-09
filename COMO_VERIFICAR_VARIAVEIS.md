# Como Verificar Variáveis de Ambiente no Windows

## ⚠️ Importante: Por que `$env:` não mostra as variáveis?

Quando você configura variáveis de ambiente no Windows, elas são salvas no **registro do sistema**, mas **não são automaticamente carregadas** no processo atual do PowerShell.

### O que acontece:
- ✅ Variáveis foram **salvas** no sistema (escopo User)
- ❌ Variáveis **não aparecem** em `$env:` no PowerShell atual
- ✅ Variáveis **estarão disponíveis** em novos processos (após reiniciar)

## ✅ Formas de Verificar

### Método 1: Script de Verificação (Recomendado)

```powershell
& ".\verificar-variaveis-ambiente.ps1"
```

Este script verifica diretamente no registro do sistema e mostra todas as variáveis configuradas.

### Método 2: Verificar no Registro (PowerShell)

```powershell
# Verificar uma variável específica
[System.Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection", "User")

# Ver todas as variáveis do usuário
[System.Environment]::GetEnvironmentVariable("Jwt__Key", "User")
[System.Environment]::GetEnvironmentVariable("ApiBaseUrl", "User")
```

### Método 3: Verificar em Novo Processo

**Após reiniciar o PowerShell ou Visual Studio:**

```powershell
# Agora $env: funcionará
$env:ConnectionStrings__DefaultConnection
$env:Jwt__Key
$env:ApiBaseUrl
```

## 🔄 Como Carregar Variáveis no Processo Atual

Se você quiser usar as variáveis **sem reiniciar**, pode carregá-las manualmente:

```powershell
# Carregar todas as variáveis do usuário no processo atual
$userVars = [System.Environment]::GetEnvironmentVariables("User")
foreach ($key in $userVars.Keys) {
    if ($key -like "*__*" -or $key -like "*ConnectionStrings*" -or $key -like "*Jwt*" -or $key -like "*Stripe*" -or $key -like "*AWS*" -or $key -like "*Mqtt*" -or $key -like "*Email*" -or $key -like "*Spotify*" -or $key -like "*Ewelink*" -or $key -like "*ApiBaseUrl*" -or $key -like "*ASPNETCORE_ENVIRONMENT*") {
        Set-Item -Path "env:$key" -Value $userVars[$key]
    }
}

# Agora você pode usar $env:
$env:ConnectionStrings__DefaultConnection
```

## 📋 Checklist

- [x] Script executado com sucesso
- [x] Variáveis configuradas no sistema
- [ ] **Reiniciar Visual Studio** (OBRIGATÓRIO)
- [ ] **Fechar e reabrir PowerShell** (OBRIGATÓRIO)
- [ ] Verificar novamente após reiniciar

## 🎯 Próximos Passos

1. **Feche este PowerShell**
2. **Abra um novo PowerShell**
3. **Execute**: `& ".\verificar-variaveis-ambiente.ps1"`
4. **Reinicie o Visual Studio**
5. **Teste a API**: `dotnet run` no projeto StarkAid.Api

## ✅ Confirmação

As variáveis **estão configuradas corretamente** no sistema. Elas só não aparecem no `$env:` porque o processo atual foi iniciado antes de configurá-las.

**Após reiniciar o Visual Studio e PowerShell, tudo funcionará!** 🎉

