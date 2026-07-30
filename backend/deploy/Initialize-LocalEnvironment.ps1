[CmdletBinding()]
param(
    [string]$EnvironmentPath = (Join-Path $PSScriptRoot ".env")
)

function New-RandomSecret {
    param([ValidateRange(32, 128)][int]$ByteLength = 48)

    $bytes = New-Object byte[] $ByteLength
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
        return [Convert]::ToBase64String($bytes)
    }
    finally {
        $generator.Dispose()
    }
}

$fullPath = [System.IO.Path]::GetFullPath($EnvironmentPath)
$directory = Split-Path -Parent $fullPath
New-Item -ItemType Directory -Path $directory -Force | Out-Null

$lines = @()
if (Test-Path -LiteralPath $fullPath) {
    $lines = @(Get-Content -LiteralPath $fullPath)
}

$existing = @{}
foreach ($line in $lines) {
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=') {
        $name = $Matches[1]
        if ($existing.ContainsKey($name)) {
            throw "La variable '$name' está duplicada en '$fullPath'. Corrija el archivo antes de continuar."
        }

        $existing[$name] = $true
    }
}

$requiredSecrets = [ordered]@{
    POSTGRES_PASSWORD             = 36
    FINANZAS_API_PASSWORD         = 36
    FINANZAS_WORKER_PASSWORD      = 36
    JWT_SIGNING_KEY               = 48
    ARCHIVOS_SIGNING_KEY          = 48
    TOKEN_PUSH_ENCRYPTION_KEY     = 48
    MINIO_ROOT_PASSWORD           = 36
}

$newLines = New-Object System.Collections.Generic.List[string]
foreach ($entry in $requiredSecrets.GetEnumerator()) {
    if (-not $existing.ContainsKey($entry.Key)) {
        $newLines.Add("$($entry.Key)=$(New-RandomSecret -ByteLength $entry.Value)")
    }
}

if (-not $existing.ContainsKey("MINIO_ROOT_USER")) {
    $newLines.Add("MINIO_ROOT_USER=finanzas_local")
}

if ($newLines.Count -eq 0) {
    Write-Host "El archivo '$fullPath' ya contiene todas las variables requeridas."
    exit 0
}

$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
if ($lines.Count -eq 0) {
    [System.IO.File]::WriteAllLines($fullPath, $newLines, $utf8WithoutBom)
}
else {
    $content = New-Object System.Collections.Generic.List[string]
    $content.AddRange([string[]]$lines)
    if ($content[$content.Count - 1] -ne "") {
        $content.Add("")
    }
    $content.Add("# Variables agregadas automáticamente; no confirmar este archivo en Git.")
    $content.AddRange($newLines)
    [System.IO.File]::WriteAllLines($fullPath, $content, $utf8WithoutBom)
}

$addedNames = $newLines |
    ForEach-Object { ($_ -split '=', 2)[0] } |
    Sort-Object

Write-Host "Se agregaron variables en '$fullPath':"
$addedNames | ForEach-Object { Write-Host "  - $_" }
Write-Host "Los valores no se muestran y el archivo está excluido de Git y Docker."
