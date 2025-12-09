# 🚀 Como Criar o Instalador StarkAid

## Passos Rápidos

### 1. Instalar Inno Setup
- Download: https://jrsoftware.org/isdl.php
- Instale a versão mais recente (6.2.2 ou superior)

### 2. Compilar o Projeto em Release
```powershell
cd StarkAid.WindowsForms
dotnet build -c Release
```

**OU** no Visual Studio:
- Selecione configuração **Release**
- Pressione `Ctrl+Shift+B` (Build Solution)

### 3. Criar o Instalador
1. Abra o **Inno Setup Compiler**
2. File > Open
3. Selecione `installer.iss` (na raiz do projeto)
4. Build > Compile (ou pressione `F9`)
5. O instalador será gerado em: `installer\StarkAid-Setup-1.0.0.exe`

## ✅ Pronto!

O instalador está em: `installer\StarkAid-Setup-1.0.0.exe`

## 📝 Personalizar Versão

Edite a linha 5 do arquivo `installer.iss`:
```iss
#define MyAppVersion "1.0.0"  ← Altere aqui
```

## 🔧 Requisitos

- Windows 10 ou superior (64-bit)
- .NET 8 Desktop Runtime (o instalador verifica e oferece download se necessário)

## 📦 O que o Instalador Inclui

- ✅ Executável principal
- ✅ Todas as DLLs necessárias
- ✅ Arquivos de som
- ✅ Licença e documentação
- ✅ Atalhos no Menu Iniciar e Desktop
- ✅ Desinstalador completo

---

**Dúvidas?** Consulte `INSTALADOR_README.md` para informações detalhadas.

