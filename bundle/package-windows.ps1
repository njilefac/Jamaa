$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$solutionRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $solutionRoot 'Jamaa.Desktop/Jamaa.Desktop.csproj'
$publishDir = Join-Path $solutionRoot 'publish/windows'
$outputFile = Join-Path $PSScriptRoot 'Jamaa-Installer.exe'
$stagingDir = Join-Path $solutionRoot 'bundle/windows-staging'
$installerScript = Join-Path $PSScriptRoot 'package-windows.nsi'
$iconPath = Join-Path $solutionRoot 'Jamaa.Desktop/Assets/Icons/jamaa.ico'

Write-Host "Building Windows package..." -ForegroundColor Yellow

foreach ($path in @($publishDir, $stagingDir, $outputFile)) {
    if (Test-Path $path) {
        Remove-Item -Recurse -Force $path
    }
}

dotnet publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -p:PublishTrimmed=true -p:TrimMode=partial -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed"
}

New-Item -ItemType Directory -Path $stagingDir | Out-Null
Copy-Item -Path "$publishDir/*" -Destination $stagingDir -Recurse

# Check if makensis is available
$makensis = Get-Command makensis -ErrorAction SilentlyContinue
if (-not $makensis) {
    Write-Host "makensis (NSIS) not found in PATH. Attempting to install via Chocolatey..." -ForegroundColor Yellow
    $choco = Get-Command choco -ErrorAction SilentlyContinue
    if ($choco) {
        & choco install nsis -y
        if ($LASTEXITCODE -eq 0) {
            # Refresh PATH for the current process
            $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
            $makensis = Get-Command makensis -ErrorAction SilentlyContinue
        }
    }
}

if (-not $makensis) {
    Write-Warning "makensis (NSIS) not found in PATH and could not be installed. Skipping installer creation."
    Write-Host "Binaries are available in: $publishDir" -ForegroundColor Cyan
    exit 0
}

& $makensis.Source `
    "/DAPP_NAME=Jamaa" `
    "/DAPP_VERSION=1.0.0" `
    "/DINPUT_DIR=$stagingDir" `
    "/DOUTPUT_FILE=$outputFile" `
    "/DICON_FILE=$iconPath" `
    $installerScript

if ($LASTEXITCODE -ne 0) {
    throw "NSIS packaging failed"
}

Remove-Item -Recurse -Force $stagingDir
