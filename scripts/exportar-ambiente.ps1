# Empacota Mongo local e fotos para levar a outro PC.
# Uso (na raiz do repositório, com a API parada):
#   powershell -File scripts/exportar-ambiente.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$mongo = Join-Path $root ".mongo-data"
$uploads = Join-Path $root "ECommerce.API\wwwroot\uploads"
$outDir = Join-Path $root "portabilidade"
$zip = Join-Path $outDir "inovatech-dados.zip"
$staging = Join-Path $outDir "_staging"

$busy = Get-NetTCPConnection -LocalPort 27017 -State Listen -ErrorAction SilentlyContinue
if ($busy) {
    throw "A API/Mongo ainda está no ar (porta 27017). Pare o `dotnet run` e rode o script de novo."
}

if (-not (Test-Path $mongo)) {
    throw "Pasta .mongo-data não encontrada. Nada para exportar."
}

if (Test-Path $staging) {
    Remove-Item $staging -Recurse -Force
}

New-Item -ItemType Directory -Path (Join-Path $staging "mongo-data") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $staging "uploads") | Out-Null
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Copy-Item (Join-Path $mongo "*") (Join-Path $staging "mongo-data") -Recurse -Force
Remove-Item (Join-Path $staging "mongo-data\mongod.lock") -ErrorAction SilentlyContinue
Remove-Item (Join-Path $staging "mongo-data\WiredTiger.lock") -ErrorAction SilentlyContinue

if (Test-Path $uploads) {
    Get-ChildItem $uploads -File | Where-Object { $_.Name -ne ".gitkeep" } | Copy-Item -Destination (Join-Path $staging "uploads")
}

@"
InovaTech / ECommerce — pacote de dados locais
Gerado: $(Get-Date -Format "yyyy-MM-dd HH:mm")
Banco: .mongo-data  (Mongo 7, database ecommerce)
Fotos: ECommerce.API/wwwroot/uploads

No outro PC, com a API parada:
  powershell -File scripts/importar-ambiente.ps1
"@ | Set-Content -Path (Join-Path $staging "LEIA-ME.txt") -Encoding UTF8

if (Test-Path $zip) {
    Remove-Item $zip -Force
}

Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zip -Force
Remove-Item $staging -Recurse -Force

Write-Host "Pacote criado: $zip"
Write-Host "Leve este ZIP junto com o código. No outro PC rode scripts/importar-ambiente.ps1"
