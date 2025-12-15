# Script para parar a aplicação e compilar
Write-Host "Parando processos StarkAid.Api..." -ForegroundColor Yellow

# Tentar parar processos
Get-Process | Where-Object { $_.ProcessName -like "*StarkAid.Api*" } | ForEach-Object {
    Write-Host "Parando processo: $($_.ProcessName) (PID: $($_.Id))" -ForegroundColor Yellow
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
}

# Aguardar um pouco
Start-Sleep -Seconds 2

Write-Host "`nCompilando projeto..." -ForegroundColor Green
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Compilação bem-sucedida!" -ForegroundColor Green
} else {
    Write-Host "`n❌ Erro na compilação. Verifique os erros acima." -ForegroundColor Red
}
