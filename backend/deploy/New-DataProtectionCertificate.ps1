[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "secrets\data-protection.pfx"),
    [ValidateRange(1, 10)]
    [int]$ValidYears = 3
)

$outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $outputFullPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

if (Test-Path -LiteralPath $outputFullPath) {
    throw "Ya existe '$outputFullPath'. Elimínelo o indique otro OutputPath para rotar el certificado conscientemente."
}

$password = Read-Host "Contraseña para proteger el PFX (guárdela fuera del repositorio)" -AsSecureString
$certificate = $null

try {
    $certificate = New-SelfSignedCertificate `
        -Subject "CN=Finanzas Inteligentes Data Protection" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyAlgorithm RSA `
        -KeyLength 3072 `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -KeySpec KeyExchange `
        -NotAfter (Get-Date).AddYears($ValidYears)

    Export-PfxCertificate `
        -Cert $certificate `
        -FilePath $outputFullPath `
        -Password $password `
        -NoProperties | Out-Null
}
finally {
    if ($null -ne $certificate) {
        Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($certificate.Thumbprint)" -Force
    }
}

Write-Host "Certificado creado en: $outputFullPath"
Write-Host "La contraseña no se guardó. Configúrela como DataProtection__CertificatePassword."
