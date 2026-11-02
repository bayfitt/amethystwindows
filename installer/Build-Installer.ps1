param(
    [Parameter(Mandatory = $true)]
    [string]$InnoCompiler,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (-not (Test-Path -LiteralPath $InnoCompiler -PathType Leaf)) {
    throw "Inno Setup compiler not found: $InnoCompiler"
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts\installer'
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$payload = Join-Path $OutputDirectory 'payload-win-arm64'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

dotnet publish (Join-Path $repoRoot 'AmethystWindows\AmethystWindows.csproj') `
    --configuration Release --runtime win-arm64 --self-contained true `
    -p:Platform=AnyCPU --output $payload --nologo
if ($LASTEXITCODE -ne 0) { throw "ARM64 publish failed with exit code $LASTEXITCODE." }

& $InnoCompiler "/DPayloadDir=$payload" "/O$OutputDirectory" (Join-Path $PSScriptRoot 'AmethystWindows.iss')
if ($LASTEXITCODE -ne 0) { throw "Installer build failed with exit code $LASTEXITCODE." }

Write-Output (Join-Path $OutputDirectory 'AmethystWindows-Setup-win-arm64-2026.11.02.1.exe')
