# GitHub Cleanup Notes

Data: 30/04/2026

## Objetivo

Registrar o que impede este repositório de parecer profissional no GitHub antes de qualquer push público.

## Problemas identificados

### 1. Artefatos gerados versionados

Há conteúdo de build e publicação versionado no repositório, incluindo:

- `.temp_build/`
- arquivos compilados `.dll`
- arquivos de publish
- logs
- artefatos intermediários

Isso polui o histórico e aumenta o tamanho do repositório sem agregar valor de código-fonte.

### 2. Configurações locais e editor versionadas

Há arquivos e pastas de ambiente/editor presentes no versionamento:

- `.vscode/`
- conteúdo Android local no subprojeto
- arquivos de configuração local

Esses arquivos não deveriam entrar como parte principal do histórico compartilhado.

### 3. Arquivos sensíveis ou operacionais a revisar

Há referências que precisam de revisão manual antes de novo push:

- `starkaidautomacao/app/google-services.json`
- `**/appsettings.Production.json`
- `VARIAVEIS_MONSTERASP.txt`
- scripts e documentos de configuração operacional

Nem todo arquivo aqui é necessariamente segredo ativo, mas todos precisam de checagem antes de serem mantidos em um repositório público.

### 4. Repositório com mudanças locais já em andamento

O diretório está com alterações não finalizadas em:

- `StarkAid.Api`
- `StarkAid.Web`
- `starkaidautomacao`

Isso significa que qualquer limpeza deve ser feita com cuidado para não apagar trabalho útil já em progresso.

## Estratégia recomendada

1. Não sair removendo arquivo do GitHub no impulso.
2. Separar primeiro o que é:
- código-fonte
- configuração local
- build gerado
- segredo/configuração sensível
3. Criar uma limpeza em commit separado.
4. Só depois revisar o que será publicado.

## Próxima ação segura

Antes de qualquer push:

1. revisar `git status`
2. conferir `README`
3. revisar arquivos sensíveis
4. confirmar explicitamente o que será enviado ao GitHub
