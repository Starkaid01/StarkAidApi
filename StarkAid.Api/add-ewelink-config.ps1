# Script para adicionar configuração Ewelink ao appsettings.Production.json
$filePath = "e:\projetos\StarkaidAPI\StarkAid\StarkAid.Api\appsettings.Production.json"

# Ler o arquivo
$content = Get-Content $filePath -Raw
$json = $content | ConvertFrom-Json

# Adicionar ou atualizar a seção Ewelink
if (-not $json.Ewelink) {
    $json | Add-Member -MemberType NoteProperty -Name "Ewelink" -Value ([PSCustomObject]@{})
}

$json.Ewelink | Add-Member -MemberType NoteProperty -Name "ClientId" -Value "qPNNDkWlhKwh4xn41bteq2qD02aiGs3D" -Force
$json.Ewelink | Add-Member -MemberType NoteProperty -Name "ClientSecret" -Value "kdG0r5OPddNB90tPKvarWyMWmpppIX9s" -Force
$json.Ewelink | Add-Member -MemberType NoteProperty -Name "RedirectUri" -Value "https://starkaidautomacao.runasp.net/auth/ewelink/callback.html" -Force

# Salvar de volta
$json | ConvertTo-Json -Depth 10 | Set-Content $filePath

Write-Host "Configuração Ewelink adicionada com sucesso!" -ForegroundColor Green
