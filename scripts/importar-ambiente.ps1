# Restaura Mongo local e fotos exportados por scripts/exportar-ambiente.ps1
# Uso (na raiz do repositório, com a API parada):
#   powershell -File scripts/importar-ambiente.ps1
#   powershell -File scripts/importar-ambiente.ps1 -Zip "C:\caminho\inovatech-dados.zip"

param(
    [string]$Zip
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not $Zip) {
    $Zip = Join-Path $root "portabilidade\inovatech-dados.zip"
}

$busy = Get-NetTCPConnection -LocalPort 27017 -State Listen -ErrorAction SilentlyContinue
if ($busy) {
    throw "A API/Mongo ainda está no ar (porta 27017). Pare o `dotnet run` e rode o script de novo."
}

if (-not (Test-Path $Zip)) {
    throw "ZIP não encontrado: $Zip"
}

$mongo = Join-Path $root ".mongo-data"
$uploads = Join-Path $root "ECommerce.API\wwwroot\uploads"
$staging = Join-Path $root "portabilidade\_staging-import"

if (Test-Path $staging) {
    Remove-Item $staging -Recurse -Force
}

Expand-Archive -Path $Zip -DestinationPath $staging -Force

$sourceMongo = Join-Path $staging "mongo-data"
$sourceUploads = Join-Path $staging "uploads"
if (-not (Test-Path $sourceMongo)) {
    throw "O ZIP não contém mongo-data. Exporte de novo neste PC com scripts/exportar-ambiente.ps1"
}

if (Test-Path $mongo) {
    $backup = Join-Path $root (".mongo-data.bak-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
    Rename-Item $mongo $backup
    Write-Host "Backup do banco atual: $backup"
}

Copy-Item $sourceMongo $mongo -Recurse
Remove-Item (Join-Path $mongo "mongod.lock") -ErrorAction SilentlyContinue
Remove-Item (Join-Path $mongo "WiredTiger.lock") -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Path $uploads -Force | Out-Null
if (Test-Path $sourceUploads) {
    Copy-Item (Join-Path $sourceUploads "*") $uploads -Force
}

Remove-Item $staging -Recurse -Force
Write-Host "Dados restaurados. Suba a API e o frontend; entre com o mesmo e-mail/senha deste PC."
