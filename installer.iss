; Script de Instalação Inno Setup para StarkAid Windows Forms
; Criado para Adriano Carmo - 2025

#define MyAppName "StarkAid"
#define MyAppVersion GetVersionNumbersString("publish\StarkAid.WindowsForms-win-x64\StarkAid.WindowsForms.exe")
#define MyAppPublisher "Adriano Carmo"
#define MyAppURL "https://starkaid.runasp.net"
#define MyAppExeName "StarkAid.WindowsForms.exe"
#define MyAppId "{{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}"

[Setup]
; Informações básicas
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE
InfoBeforeFile=README.md
OutputDir=installer
OutputBaseFilename=StarkAid-Setup-{#MyAppVersion}
SetupIconFile=icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763

; Aparência
WizardImageFile=
WizardSmallImageFile=
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
; Adicione o idioma português se tiver o arquivo Portuguese.isl instalado
; Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
; Quick Launch icon removido (não suportado em versões modernas do Windows)
; Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; ========================================
; INCLUIR TODOS OS ARQUIVOS DA PASTA RELEASE SEM EXCEÇÃO
; ========================================
; Isso garante que WebView2, runtimes e todas as dependências sejam incluídas
; Copiando EXATAMENTE como está na pasta release

; INCLUIR TUDO do publish (todos os arquivos e pastas)
Source: "publish\StarkAid.WindowsForms-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Arquivos de configuração e documentação (da raiz do projeto)
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme

; NOTA: Todos os arquivos da pasta release são incluídos, incluindo:
; - Executável e DLLs
; - Pasta runtimes completa
; - Pasta StarkAid.WindowsForms.exe.WebView2 completa
; - Arquivos JSON, XML, ICO
; - Arquivos de som (efectsound)
; - Arquivos .pdb (se existirem)
; - Qualquer outro arquivo presente na pasta release

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Executar aplicativo após instalação
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]

function InitializeUninstall(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  
  // Parar o aplicativo se estiver em execução
  if Exec('taskkill', '/F /IM ' + ExpandConstant('{#MyAppExeName}'), '', SW_HIDE, ewWaitUntilTerminated, ErrorCode) then
  begin
    // Aplicativo foi fechado
  end;
end;

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Database"
Type: filesandordirs; Name: "{app}\logs"
Type: files; Name: "{app}\*.db"
Type: files; Name: "{app}\*.db-shm"
Type: files; Name: "{app}\*.db-wal"

[Registry]
; Registros opcionais (se necessário)
; Root: HKCU; Subkey: "Software\{#MyAppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletevalue

[CustomMessages]
; Mensagens personalizadas em inglês (padrão)
LicenseLabel3=Authorized use only by the licensee. Any unauthorized use, copying or distribution is prohibited.
InfoBeforeLabel=StarkAid - Home Automation System%n%nThis software is provided exclusively for authorized use by the licensee.%n%nCopying, redistribution, sublicensing, reverse engineering, modification or commercial use without prior written authorization from the licensee is prohibited.

