# Instalador StarkAid - Guia de Uso

Este documento explica como criar o instalador do StarkAid Windows Forms usando Inno Setup.

## 📋 Pré-requisitos

1. **Inno Setup** instalado (versão 6.0 ou superior)
   - Download: https://jrsoftware.org/isdl.php
   - Versão recomendada: Inno Setup 6.2.2 ou superior

2. **Projeto compilado em Release**
   - Certifique-se de que o projeto foi compilado em modo Release
   - O executável deve estar em: `StarkAid.WindowsForms\bin\Release\net8.0-windows\`

3. **Arquivos necessários no diretório raiz:**
   - `LICENSE` - Arquivo de licença
   - `README.md` - Documentação do projeto

## 🔧 Como Criar o Instalador

### Passo 1: Compilar o Projeto

```powershell
cd StarkAid.WindowsForms
dotnet publish -c Release -r win-x64 --self-contained false
```

**OU** compilar via Visual Studio:
- Selecione a configuração **Release**
- Build > Build Solution (Ctrl+Shift+B)

### Passo 2: Abrir o Script Inno Setup

1. Abra o Inno Setup Compiler
2. File > Open
3. Selecione o arquivo `installer.iss` na raiz do projeto

### Passo 3: Ajustar Caminhos (se necessário)

Se a estrutura de pastas for diferente, ajuste as seções `[Files]` no script:

```iss
Source: "StarkAid.WindowsForms\bin\Release\net8.0-windows\StarkAid.WindowsForms.exe"; DestDir: "{app}"; Flags: ignoreversion
```

### Passo 4: Compilar o Instalador

1. No Inno Setup Compiler, clique em **Build > Compile** (F9)
2. O instalador será gerado em: `installer\StarkAid-Setup-1.0.0.exe`

## 📦 Estrutura do Instalador

O instalador inclui:

- ✅ Executável principal (`StarkAid.WindowsForms.exe`)
- ✅ Todas as DLLs necessárias
- ✅ Arquivos de som (`efectsound\*.mp3`)
- ✅ Arquivos de configuração
- ✅ Licença e README
- ✅ Verificação de .NET 8 Runtime
- ✅ Atalhos no Menu Iniciar e Desktop
- ✅ Desinstalador completo

## 🔍 Verificações Automáticas

O instalador verifica automaticamente:

1. **.NET 8 Desktop Runtime**: Se não estiver instalado, oferece download
2. **Permissões de Administrador**: Requeridas para instalação
3. **Versão do Windows**: Mínimo Windows 10 (versão 1809)

## ⚙️ Personalização

### Alterar Versão

Edite a linha no início do arquivo `installer.iss`:

```iss
#define MyAppVersion "1.0.0"
```

### Adicionar Ícone

1. Crie ou obtenha um arquivo `.ico`
2. Coloque na raiz do projeto
3. Descomente e ajuste a linha:

```iss
SetupIconFile=logo.ico
```

### Alterar Informações do Publicador

Edite as linhas no início do arquivo:

```iss
#define MyAppPublisher "Adriano Carmo"
#define MyAppURL "https://starkaid.runasp.net"
```

## 🚀 Distribuição

Após compilar, o instalador estará em:

```
installer\StarkAid-Setup-1.0.0.exe
```

Este arquivo pode ser distribuído diretamente aos usuários.

## 📝 Notas Importantes

1. **.NET 8 Runtime**: O aplicativo requer .NET 8 Desktop Runtime. O instalador verifica e oferece download se necessário.

2. **Banco de Dados Local**: O banco de dados SQLite será criado automaticamente na primeira execução em:
   ```
   %LocalAppData%\StarkAid\
   ```

3. **Dados do Usuário**: Durante a desinstalação, os dados locais (banco de dados, configurações) são removidos.

4. **Atualizações**: Para atualizar, o usuário deve desinstalar a versão anterior e instalar a nova.

## 🐛 Solução de Problemas

### Erro: "Cannot find source file"

- Verifique se o projeto foi compilado em modo Release
- Confirme que o caminho no script está correto
- Certifique-se de que todos os arquivos necessários existem

### Erro: ".NET Runtime not found"

- O instalador oferece download automático
- Ou instale manualmente: https://dotnet.microsoft.com/download/dotnet/8.0

### Instalador muito grande

- O instalador inclui todas as DLLs necessárias
- Para reduzir tamanho, considere usar `--self-contained false` (já configurado)

## 📞 Suporte

Para problemas ou dúvidas sobre o instalador, consulte:
- Documentação do Inno Setup: https://jrsoftware.org/ishelp/
- README do projeto: `README.md`

---

**Desenvolvido por:** Adriano Carmo  
**Ano:** 2025

