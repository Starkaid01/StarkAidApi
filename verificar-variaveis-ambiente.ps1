# Script para verificar se todas as variaveis de ambiente estao configuradas

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verificando Variaveis de Ambiente" -ForegroundColor Cyan
Write-Host "StarkAid API" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$variaveis = @(
    "ConnectionStrings__DefaultConnection",
    "Jwt__Key",
    "Jwt__Issuer",
    "Jwt__Audience",
    "Firebase__CredentialsPath",
    "StripeSettings__SecretKey",
    "StripeSettings__PublishableKey",
    "StripeSettings__WebhookSecret",
    "StripeSettings__PriceIdNivel2",
    "StripeSettings__PriceIdNivel3",
    "StripeSettings__PriceIdNivel4",
    "StripeSettings__PriceIdNivel5",
    "StripeSettings__PriceIdNivel6",
    "StripeSettings__PriceIdNivel7",
    "StripeSettings__CheckoutFrontendUrl",
    "StripeSettings__AppDeepLink",
    "StripeSettings__SoftwareDeepLink",
    "AWS__AccessKey",
    "AWS__SecretKey",
    "AWS__Region",
    "AWS__Profile",
    "IaApiKeys__GroApiKey",
    "IaApiKeys__OpenRouterKEY",
    "Mqtt__Broker",
    "Mqtt__Port",
    "Mqtt__Username",
    "Mqtt__Password",
    "EmailSettings__From",
    "EmailSettings__SmtpServer",
    "EmailSettings__Port",
    "EmailSettings__Username",
    "EmailSettings__Password",
    "WppConnectOptions__BaseUrl",
    "WppConnectOptions__TokenDeAutenticacao",
    "WppConnectOptions__NovoDominio",
    "NlpConnectOptions__BaseUrl",
    "NlpConnectOptions__TokenDeAutenticacao",
    "NlpConnectOptions__NovoDominio",
    "Tuya__AccessId",
    "Tuya__AccessSecret",
    "Tuya__BaseUrl",
    "Tuya__CountryCode",
    "ApiBaseUrl",
    "Spotify__ClientId",
    "Spotify__ClientSecret",
    "Spotify__RedirectUri",
    "Ewelink__ClientId",
    "Ewelink__ClientSecret",
    "Ewelink__RedirectUri",
    "ASPNETCORE_ENVIRONMENT"
)

$configuradas = 0
$faltando = @()

foreach ($var in $variaveis) {
    $valor = [System.Environment]::GetEnvironmentVariable($var, "User")
    if ([string]::IsNullOrEmpty($valor)) {
        $valor = [System.Environment]::GetEnvironmentVariable($var, "Machine")
    }
    
    if ([string]::IsNullOrEmpty($valor)) {
        Write-Host "[X] $var" -ForegroundColor Red
        $faltando += $var
    } else {
        Write-Host "[OK] $var" -ForegroundColor Green
        $configuradas++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Resumo" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
$color = if ($configuradas -eq $variaveis.Count) { "Green" } else { "Yellow" }
Write-Host "Configuradas: $configuradas / $($variaveis.Count)" -ForegroundColor $color
Write-Host ""

if ($faltando.Count -gt 0) {
    Write-Host "Variaveis faltando:" -ForegroundColor Red
    foreach ($var in $faltando) {
        Write-Host "  - $var" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Execute o script configurar-variaveis-ambiente.ps1 para configurar." -ForegroundColor Yellow
} else {
    Write-Host "Todas as variaveis estao configuradas!" -ForegroundColor Green
}

Write-Host ""
